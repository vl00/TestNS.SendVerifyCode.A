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
using static TestNs.PuppeteerSharp.Jobs.SendVeCodeToPhoneNumJobHandler;

namespace TestNs.PuppeteerSharp.Jobs;

partial class Svc_tianyancha(IServiceProvider serviceProvider)
{
    readonly BrowserService _browserService = serviceProvider.GetService<BrowserService>();

    public async Task OnHandle(SendVeCodeToPhoneNumJob job, JobContext ctx, CancellationToken cancellation)
    {
        var url = "https://www.tianyancha.com/login?from=%2F404";

        using var page = await _browserService.NewPageAndInit();
        await page.EvaluateExpressionOnNewDocumentAsync(await File.ReadAllTextAsync("html2canvas.min.js"));
        await page.EvaluateExpressionOnNewDocumentAsync("()=>localStorage.clear()");
        await page.GoToAsync(url);
        await page.DeleteCookieAsync(await page.GetCookiesAsync());
        await Task.Delay(1500, cancellation);

        var (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('div.toggle_box')][0] "); 
        await ele.ClickAsync();
        await Task.Delay(1200, cancellation);

        (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('div')].filter(x=>x.innerText?.indexOf('短信')>-1).filter((x,_,arr) => arr.findIndex(x2 => x2.parentElement == x) == -1)[0] ");
        await ele.ClickAsync();
        await Task.Delay(800, cancellation);

        ele = await page.ExGetElement("c_div", " [...document.querySelectorAll('div.modulein')].filter(x=>x.classList.contains('in'))[0] ");

        (ele, clss) = await page.ExGetElement(" [...document.querySelectorAll('.c_div input')].filter(x=>x.getAttribute('placeholder')?.indexOf('手机号')>-1)[0] ");
        await ele.ClickAsync();
        await page.Keyboard.TypeAsync(job.PhoneNum, new() { Delay = 100 });
        await Task.Delay(200, cancellation);


        (ele, clss) = await page.ExGetElement($" [...document.querySelectorAll('.c_div .input-group-btn')].filter(a=>a.innerText?.indexOf('验证码')>-1)[0] ");
        await ele.ClickAsync();
        await Task.Delay(200, cancellation);

        var ierr = await CheckOk();
        if (ierr == 0) return;
        ctx.Logger.LogDebug("直接获取vcode失败,ierr={ierr}", ierr);
        if (ierr is not (1 or 2))
        {
            throw new Exception("直接获取vcode失败");
        }

        if (ierr == 1)
        {
            await Task.Delay(2500, cancellation); 
            (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('.geetest_box')].filter(x=>x.style.display!='none')[0] ");
            var b64 = await page.ExGetBase64ByScreenshot($".{clss}");
            if (string.IsNullOrEmpty(b64))
            {
                throw new Exception("not any img for dianxuan");
            }
            await File.WriteAllBytesAsync("dx.1.png", Convert.FromBase64String(b64)); 
            var arr = await GetWeiZhiByTe(b64, -3);
            ctx.Logger.LogDebug("resolve arr='{arr}'.", arr.ToJsonStr()); 
            // 
            var box = await ele.BoundingBoxAsync();
            foreach (var pa in arr)
            {
                var (x, y) = ((box.X + ((decimal)pa[2] + (decimal)pa[0]) / 2), (box.Y + ((decimal)pa[3] + (decimal)pa[1]) / 2));
                await page.Mouse.MoveAsync(x, y);
                await page.Mouse.DownAsync(new() { Delay = 100 });
                await page.Mouse.UpAsync(new() { Delay = 100 });
                await Task.Delay(500, cancellation);
            }
            //
            (ele, _) = await page.ExGetElement(" [...document.querySelectorAll('div.geetest_submit')][0] ");
            await ele.ClickAsync();
            await Task.Delay(1000, cancellation);
            //
            var ierr2 = await CheckOk();
            if (ierr2 == 0) return;
            throw new Exception($"dx ierr2={ierr2}");
        }

        if (ierr == 2)
        {
            var x = new Huakuai_v1_1("天眼查", page);
            {
                x.CheckMove = CheckMove;
                x.BgSelector = ".geetest_bg";
                x.HuaKuaiSelector = ".geetest_slice";
                x.TuoSelector = ".geetest_btn";
                x.RetryCount = 0;
                x.HuaKuaiStyleLeft = ".transform.split(',')[0]";
                x.FuncGetBytesForBgSelector = async () =>
                {
                    var b64 = await page.EvaluateFunctionAsync<string>($$"""
                    async ()=>{
                    document.querySelector('{{x.HuaKuaiSelector}}').style.display = 'none';
                    document.querySelector('{{x.BgSelector}}').style.display = 'block';
                    var c = await html2canvas(document.querySelector('{{x.BgSelector}}'));
                    return c.toDataURL().replace(/^data:image\/(\w+);base64,/g,'');
                    }
                    """);
                    return Convert.FromBase64String(b64);
                };
                x.FuncGetBytesForHuaKuaiSelector = async () =>
                {
                    var b64 = await page.EvaluateFunctionAsync<string>($$"""
                    async ()=>{
                    document.querySelector('{{x.HuaKuaiSelector}}').style.display = 'block';
                    var c = await html2canvas(document.querySelector('{{x.HuaKuaiSelector}}'));
                    return c.toDataURL().replace(/^data:image\/(\w+);base64,/g,'');
                    }
                    """);
                    return Convert.FromBase64String(b64);
                };
            }
            var b = await x.RunAsync(ctx, cancellation);
            if (b == 0) return;
            throw new Exception($"hk err");
        }

        async Task<int> CheckOk()
        {
            var ok = -1;
            for (var i = 0; i < 3; i++)
            {
                ok = await page.EvaluateFunctionAsync<int>($$"""
                    () => {
                    let a = document.querySelector('.{{clss}}') || document.querySelector('.c_div .input-group-btn');
                    let s = a?.innerText?.toLowerCase() || '';
                    if (s.indexOf('s') > -1 || s.indexOf('秒') > -1 || s.indexOf('重') > -1) return 0;
                    a = [...document.querySelectorAll('.geetest_box')].filter(x=>x.style.display!='none')[0];
                    if (a) {
                        a = [...document.querySelectorAll('.geetest_box .geetest_slider')][0];
                        if (a) return 2;
                        return 1;
                    }
                    return -1;
                    }
                    """);
                if (ok == 0) break;
                await Task.Delay(1000, cancellation);
            }
            return ok;
        }

        async Task<int> CheckMove()
        {
            var ok = -1;
            for (var i = 0; i < 3; i++)
            {
                ok = await page.EvaluateFunctionAsync<int>($$"""
                    ()=>{
                    let a = document.querySelector('.{{clss}}') || document.querySelector('.c_div .input-group-btn');
                    let s = a?.innerText?.toLowerCase() || '';
                    if (s.indexOf('s') > -1 || s.indexOf('秒') > -1 || s.indexOf('重') > -1) return 0;
                    return -1;
                    }
                    """);
                if (ok == 0) break;
                await Task.Delay(1000, cancellation);
            }
            return ok;
        }
    }

}

