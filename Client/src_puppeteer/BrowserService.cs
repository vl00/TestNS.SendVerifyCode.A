using Common;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program_ext_file;

namespace TestNs.PuppeteerSharp;

public class BrowserService(IConfiguration config)
{
    IBrowser _browser;

    public IBrowser GetPuppeteerBrowser() => _browser;
    public IBrowser GetBrowser() => _browser;

    static LaunchOptions GetLaunchOptions(IConfiguration config)
    {
        var launchOptions = new LaunchOptions { Headless = bool.Parse(config["browser:headless"]) };
        {
            launchOptions.ExecutablePath = config["browser:exe"];
            launchOptions.UserDataDir = Path.Combine(Directory.GetCurrentDirectory(), "User Data").TrimEnd('\\').TrimEnd('/');     
        }
        launchOptions.DumpIO = false;
        launchOptions.IgnoreHTTPSErrors = true;
        launchOptions.Args = new[]
        {
            "--no-sandbox", "--disable-setuid-sandbox",
            "--disable-web-security", "--ignore-urlfetcher-cert-requests", "--ignore-certificate-errors",
            "--disable-blink-features=AutomationControlled",
            "--disable-infobars",
            "--disable-extensions",
        };
        //launchOptions.IgnoreDefaultArgs = true;
        launchOptions.IgnoredDefaultArgs = new[]
        {
            "--enable-automation",
        };
        launchOptions.DefaultViewport = new ViewPortOptions { IsMobile = false };    
        return launchOptions;
    }

    public async Task InitBrowser()
    {
        if (_browser != null) return;

        var launchOptions = GetLaunchOptions(config);
        
        await KillBefore(launchOptions);
        await CreateBrowser();

        async Task CreateBrowser()
        {
            while (true)
            {
                try
                {
                    _browser = await Puppeteer.LaunchAsync(launchOptions);
                    _browser.Closed += _browser_Closed;

                    var pages = await _browser.PagesAsync();
                    if (pages?.Length > 0)
                    {
                        await _browser.NewPageAsync();
                        await pages[0].CloseAsync();
                    }

                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"start chromium fail: {ex.Message}");
                    _browser_Closed(_browser, null);
                    await Task.Delay(1000);
                }
            }
        }
    }

    static Task KillBefore(LaunchOptions launchOptions)
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            
        }
        // ...
        //
        return Task.CompletedTask;
    }

    void _browser_Closed(object sender, EventArgs e)
    {
        if (sender is IBrowser b) b.Closed -= _browser_Closed;
        _browser?.Dispose();
        _browser = null;
    }

    public async Task<IPage> NewPageAndInit()
    {
        var page = await _browser.NewPageAsync();
        await InitPage(page);
        return page;
    }

    public async Task InitPage(IPage page)
    {
        if (page?.IsClosed != false) return;

        var args = new 
        {
			userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36",
        };

        await page.SetUserAgentAsync(args.userAgent);


    }



    public void OpenNewBrowser(string url, out Task onBrowserClosedTask)
    {
        onBrowserClosedTask = null;
        var launchOptions = GetLaunchOptions(config);
        try
        {
            //await Puppeteer.LaunchAsync(launchOptions); 

            var p = Process.Start(new ProcessStartInfo(launchOptions.ExecutablePath)
            {
                Arguments = $$""" --new-window "{{url}}" --user-data-dir="{{launchOptions.UserDataDir}}" """
            });
            onBrowserClosedTask = p.WaitForExitAsync();
            _ = onBrowserClosedTask.ContinueWith(t =>
            {
                
            });
            
        }
        catch (Exception ex)
        {
        }        
    }
    
    public async Task SaveImageToLocal(string filePath, string url)
    {
        var dir = Path.GetDirectoryName(filePath);
        Dir_MakeSureExists(dir);
        if (!string.IsNullOrEmpty(url))
        {            
            File_Del_NoError(filePath);

            var bys = await _browser.ExGetImageBytesByUrl(url);
            if (bys != null) await File.WriteAllBytesAsync(filePath, bys);
        }
    }
}
