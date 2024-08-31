using Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TestNS;

public class ProcessOption
{
    public string Address;
    public string Exe;
    public string A;
    public string Wd;
}

public class ProcessManager(ILogger<ProcessManager> log, ProcessOption opt)
    : IDisposable
{
    readonly List<Process> _ps = new(8);
    readonly ProcessOption _opt = opt;
    
    public void Start(int pc, int tc = 1)
    {
        lock (_ps)
        {
            if (_ps.Any(_ => _?.StartInfo?.Environment?["tc"] != $"{tc}"))
            {
                for (var i = 0; i < _ps.Count; i++)
                {
                    if (_ps[i] == null) continue;
                    _ps[i].Exited -= _p_Exited;
                    Kill(_ps[i]);
                }
                _ps.Clear();
            }
            
            var diff = pc - _ps.Count;
            if (diff > 0) // +
            {
                for (var i = 0; i < diff; i++)
                {
                    var xid = DateTime.Now.Ticks;
                    StartProcess(_opt.Address, tc, xid);
                }
            }
            else if (diff < 0) // -
            {
                diff = Math.Abs(diff);
                for (var i = 0; i < diff; i++)
                {
                    var p = _ps[_ps.Count - 1 - i];
                    _ps[_ps.Count - 1 - i] = null;
                    if (p == null) continue;
                    p.Exited -= _p_Exited;
                    Kill(p);
                }
                _ps.RemoveRange(pc, diff);
            }
        }
    }
    
    void StartProcess(string address, int tc, long xid)
    {
        var i = _ps.FindIndex(_ => _ == null);
        var psi = new ProcessStartInfo()
        {
            FileName = _opt.Exe, 
            Arguments = $""" --address "{address}" --tc {tc} -i {(i == -1 ? _ps.Count : i)} """,

            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
        };
        var exeDir = Path.GetDirectoryName(_opt.Exe);
        var aDir = Path.GetFullPath(_opt.A).Replace('\\', '/').TrimEnd('/');
        if (_opt.Wd != null)
        {
            psi.WorkingDirectory = Path.Combine(_opt.Wd, $"{(i == -1 ? _ps.Count : i)}").Replace('\\', '/');
            if (!Directory.Exists(psi.WorkingDirectory)) Directory.CreateDirectory(psi.WorkingDirectory);
            //Console.WriteLine(aDir);
            if (!File.Exists($"{aDir}/appsettings.json")) aDir = _opt.Wd;
            File.Copy($"{aDir}/appsettings.json", $"{psi.WorkingDirectory}/appsettings.json", true);
            File.Copy($"{aDir}/nlog.config", $"{psi.WorkingDirectory}/nlog.config", true);
        }
        //
        {
            //psi.RedirectStandardInput = psi.RedirectStandardOutput = psi.RedirectStandardError = true;
            //psi.StandardInputEncoding = psi.StandardOutputEncoding = psi.StandardErrorEncoding = Encoding.UTF8;
        }
        //
        psi.Environment.Add("exe-dir", aDir);
        psi.Environment.Add("xid", xid.ToString());
        psi.Environment.Add("tc", tc.ToString());
        //
        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        p.Exited += _p_Exited;
        p.Start();
        
        if (i > -1) _ps[i] = p;
        else _ps.Add(p);
    }
    
    void _p_Exited(object sender, EventArgs e)
    {
        var p = sender as Process;
        var xid = p.StartInfo.Environment["xid"];
        if (string.IsNullOrEmpty(xid)) return;
        log.LogDebug("process xid={xid} exit", xid);
        lock (_ps)
        {
            var i = _ps.FindIndex(_ => _?.StartInfo?.Environment?["xid"] == xid);
            if (i == -1) return;
            _ps[i] = null;
        }
        Kill(p);
    }
    
    static void Kill(Process p)
    {
        ProcessUtil.KillTree(p.Id);
        try { p.Kill(); } catch { }
        p.Dispose();
    }

    public void KillAll()
    {
        if (_ps.Count == 0) return;
        for (var i = 0; i < _ps.Count; i++)
        {
            var p = _ps[i];
            if (p == null) continue;
            p.Exited -= _p_Exited;
            Kill(p);
        }
        _ps.Clear();
    }

    public void Dispose() => KillAll();
}
