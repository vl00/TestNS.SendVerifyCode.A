using Common;
using Common.JsonNet.v2;
using Common.SimpleJobs;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TestNs.PuppeteerSharp.Jobs;

partial class Svc_大众点评(IServiceProvider serviceProvider)
{
    readonly BrowserService _browserService = serviceProvider.GetService<BrowserService>();

    public async Task OnHandle(SendVeCodeToPhoneNumJob job, JobContext ctx, CancellationToken cancellation)
    {
        var url = "https://account.dianping.com/pclogin?redir=https://m.dianping.com/dphome";

        using var page = await _browserService.NewPageAndInit();
        await page.EvaluateFunctionOnNewDocumentAsync("()=>localStorage.clear()");
		await page.DeleteCookieAsync(await page.GetCookiesAsync("https://account.dianping.com"));	
		await page.DeleteCookieAsync(await page.GetCookiesAsync("https://m.dianping.com"));	
        await page.GoToAsync(url);
        await Task.Delay(1000, cancellation);


        var (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('.pc-icon')][0] ");
        await ele.ClickAsync();
        await Task.Delay(200, cancellation);

        (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('div')].filter(x=>x.innerText?.indexOf('短信登录')>-1).filter((x,_,arr)=>arr.findIndex(x2=>x2.parentElement==x)==-1).filter(x=>x.clientWidth>1)[0] ");
        await ele.ClickAsync();
        await Task.Delay(200, cancellation);

        (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('input')].filter(x=>x.getAttribute('placeholder')?.indexOf('手机号')>-1)[0] ");
        await ele.ClickAsync();
        await page.Keyboard.TypeAsync(job.PhoneNum, new() { Delay = 100 });
        await Task.Delay(200, cancellation);

        (ele, _) = await page.ExGetElement(" [...document.querySelectorAll('label')].filter(x=>x.getAttribute('for'))[0] ");
        await ele.ClickAsync();
        await Task.Delay(200, cancellation);

        (ele, clss) = await page.ExGetElement(" [...document.querySelectorAll('button')].filter(x=>x.innerText?.indexOf('发送')>-1)[0] ");
        await page.ExGetElement("c_btn", $" document.querySelector('.{clss}').parentElement ");
        await ele.ClickAsync();
        await Task.Delay(1000, cancellation);

        (var e1, _) = await page.ExWaitForElement(" [...document.querySelectorAll('.boxStatic')][0] "); 
        (var e2, _) = await page.ExWaitForElement(" document.querySelector('.box-wrapper') "); 

        var x = (e2.BoundingBoxAsync().Result).Width + (e2.BoundingBoxAsync().Result).X;
        var y = 0m + (e2.BoundingBoxAsync().Result).Y;
        ctx.Logger.LogDebug("will drop move x={x}, y={y}", x, y);
        for (var i = 0; i < 3; i++)
        {
            await e1.ClickAsync();
            await Task.Delay(1000, cancellation);
        }
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y, new() { Steps = 5 });
        await Task.Delay(2000, cancellation);
        await page.Mouse.UpAsync();

        var ierr = await CheckOk();
        if (ierr == 0) return;
        else
        {
            throw new Exception($"移动滑块失败,ierr={ierr}");
        }

        async Task<int> CheckOk()
        {
            var ok = await page.EvaluateFunctionAsync<int>("""
                () => {
                var a = document.querySelector('.c_btn');
                var s = a?.innerText?.toLowerCase() || '';
                if (s.indexOf('s') > -1 || s.indexOf('秒') > -1 || s.indexOf('重') > -1) return 0;
                return 1;
                }
                """);
            return ok;
        }
    }

}

