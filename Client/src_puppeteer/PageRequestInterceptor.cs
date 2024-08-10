using Common;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestNs.PuppeteerSharp;

public sealed class PageRequestInterceptor : IDisposable
{
    IPage _page;
    readonly object Sync = new();
    List<Func<IPage, RequestEventArgs, Func<Task>, Task>> _ls4Add, _ls4Run;
    readonly ILogger _log;

    private PageRequestInterceptor(IPage page, ILogger logger)
    {
        _page = page;
        _page.Request += Page_Request;
        _log = logger;
    }

    public static async Task<PageRequestInterceptor> Use(IPage page, ILogger logger = null)
    {
        var pi = new PageRequestInterceptor(page, logger);
        await page.SetRequestInterceptionAsync(true);
        return pi;
    }

    public IDisposable Use(Func<IPage, RequestEventArgs, Func<Task>, IDisposable, Task> middler)
    {
        IDisposable d = null;
        return d = Use((page, e, next) => middler(page, e, next, d));
    }

    public IDisposable Use(Func<IPage, RequestEventArgs, Func<Task>, Task> middler)
    { 
        lock (Sync)
        {
            ResolveLsOnUpdate();

            _ls4Add.Add(middler);
        }

        return new Disposable(this, middler);
    }

    async void Page_Request(object sender, RequestEventArgs e)
    {
        if (_page.IsClosed) return;

        var ls = GetLsOnRun();

        try { await CallAsync(_page, e, ls); }
        catch (Exception ex) 
        {
            _log?.LogError(ex, "Page_Request call has errors");
        }
    }

    List<Func<IPage, RequestEventArgs, Func<Task>, Task>> GetLsOnRun()
    {
        List<Func<IPage, RequestEventArgs, Func<Task>, Task>> ls = null;
        lock (Sync)
        {
            ls = _ls4Run;
            if (ls == null)
            {
                ls = _ls4Run = _ls4Add;
                _ls4Add = null;
            }
        }
        return ls;
    }

    void ResolveLsOnUpdate()
    {
        if (_ls4Add != null) return;
        if (_ls4Run == null) _ls4Add = new(8);
        else
        {
            _ls4Add = _ls4Run.ToList();
            _ls4Run = null;
        }
    }

    static async Task CallAsync(IPage page, RequestEventArgs e,
        List<Func<IPage, RequestEventArgs, Func<Task>, Task>> middlers, int i = 0)
    {
        if (i < (middlers?.Count ?? 0))
        {
            var ii = i;
            try
            {
                var m = middlers[i];                

                var t = m.Invoke(page, e, () =>
                {
                    ii = i + 1;
                    return CallAsync(page, e, middlers, ii);
                });
                if (t?.IsCompletedSuccessfully == false) await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
            }
            return;
        }

        await e.Request.ContinueAsync();
    }

    public void Dispose()
    {
        if (_page == null) return;
        _page.Request -= Page_Request;
        _page = null;
        _ls4Add = _ls4Run = null;
    }

    sealed class Disposable : IDisposable
    {
        PageRequestInterceptor _this;
        Func<IPage, RequestEventArgs, Func<Task>, Task> _middler;

        public Disposable(PageRequestInterceptor i, Func<IPage, RequestEventArgs, Func<Task>, Task> m)
        {
            _this = i;
            _middler = m;
        }

        public void Dispose() 
        {
            var _this = this._this;
            if (_this == null) return;
            this._this = null;

            lock (_this.Sync)
            {
                _this.ResolveLsOnUpdate();
                _this._ls4Add.Remove(_middler);
                _middler = null;
            }
        }
    }
}
