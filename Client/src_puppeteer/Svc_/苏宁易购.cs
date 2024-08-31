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

partial class Svc_苏宁易购(IServiceProvider serviceProvider)
{
    readonly BrowserService _browserService = serviceProvider.GetService<BrowserService>();

    public async Task OnHandle(SendVeCodeToPhoneNumJob job, JobContext ctx, CancellationToken cancellation)
    {
        var url = "https://passport.suning.com/ids/login";

        using var page = await _browserService.NewPageAndInit();

        await page.GoToAsync(url);

        await page.WaitForFunctionAsync("""
            ()=>{
            var a = Array.from(document.querySelectorAll('a.tab-item')).filter(_=>_.innerText.indexOf('账户登录')>-1)[0];
            if (a) a.id = 'tmp_a';
            return a != null;
            }
            """);
        await page.ClickAsync("#tmp_a");
        await Task.Delay(200, cancellation);

        await page.ClickAsync("a.code-login");
        await Task.Delay(200, cancellation);

        await page.TypeAsync("#phoneNumber", job.PhoneNum, new() { Delay = 100 });


        await page.ClickAsync(".send-sms");
        await Task.Delay(1000, cancellation);
        var ok = await CheckOk();
        if (ok == 0) return;


        await page.ClickAsync("#iar1dx_sncaptcha_button");
        await Task.Delay(500, cancellation);


        var x = new Huakuai_v1("苏宁易购", page);
        {
            x.CheckMove = CheckMove;
            x.JsfuncOnBeforeEach = (_) =>
            {
                return """
                    ()=>{
                    var div = document.querySelector('.tobe-obfuscate-slide-main');
                    if (!div || (div.style.display || 'block').indexOf('block')==-1) return false;
                    var c = document.querySelector('.tobe-obfuscate-slide-main .slide-canvas');
                    return c != null;
                    }
                    """;
            };
            x.JsfuncOnSetId = (_) =>
            {
                return $$"""
                    ()=> {
                    document.querySelector('.tobe-obfuscate-slide-main .slide-canvas').id = 'c_bg';
                    document.querySelector('.tobe-obfuscate-slide-main .tobe-obfuscate-image-fragment').id = 'c_hk';
                    document.querySelector('.tobe-obfuscate-slide-main .tobe-obfuscate-slider').id = 'c_tuo';
                    }
                    """;
            };
        }
        var b = await x.RunAsync(ctx, cancellation);
        if (!b) throw new Exception($"{x.JobName},滑块验证码操作失败");

        await page.ClickAsync(".send-sms");
        await Task.Delay(1000, cancellation);
        ok = await CheckOk();
        if (ok == 0) return;
        else throw new Exception("苏宁易购 fail");

        async Task<int> CheckOk()
        {
            for (var i = 0; i < 6; i++)
            {
                var ok = await page.EvaluateFunctionAsync<int>("""
                    () => {
                    var x = document.querySelector('.send-sms')?.innerText?.toLowerCase() || '';
                    if (x.indexOf('s') > -1 || x.indexOf('秒') > -1) return 0;
                    x = document.querySelector('#iar1dx_sncaptcha_button')?.innerText || '';
                    if (x && !(x.indexOf('完成')>-1 || x.indexOf('已')>-1)) return 1;
                    return -1;
                    }
                    """);
                if (ok >= 0) return ok;
                await Task.Delay(1000, cancellation);
                await page.ClickAsync(".send-sms");
                await Task.Delay(1000, cancellation);
            }
            throw new Exception("苏宁易购 发送失败1");
        }
        async Task<bool> CheckMove()
        {
            var ok = await page.EvaluateFunctionAsync<bool>("""
                ()=>{
                var x = document.querySelector('#iar1dx_sncaptcha_button').innerText;
                if (x.indexOf('完成')>-1 || x.indexOf('已')>-1) return true;
                x = document.querySelector('.validate-fail');
                if (x != null) return false;
                x = parseInt(document.querySelector('#c_tuo').style.left,10);
                if (x == 0) return false;
                return false;
                }
                """);
            return ok;
        }
    }

}
