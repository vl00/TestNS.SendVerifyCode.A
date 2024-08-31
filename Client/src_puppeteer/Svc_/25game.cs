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

partial class Svc_25game(IServiceProvider serviceProvider)
{
    readonly BrowserService _browserService = serviceProvider.GetService<BrowserService>();

    public async Task OnHandle(SendVeCodeToPhoneNumJob job, JobContext ctx, CancellationToken cancellation)
    {
        var url = "https://www.25game.com/User/Register/";

        using var page = await _browserService.NewPageAndInit();

        await page.EvaluateFunctionOnNewDocumentAsync(JsFuncStr("""
            window.alert = (s) => {
                console.log('alert', s);
                window.__alert_msg = s;
            };
            window.__get_alert_msg = () => {
                let s = window.__alert_msg;
                window.__alert_msg = undefined;
                return s;
            };
            """));

        await page.GoToAsync(url);
        await Task.Delay(1000, cancellation);


        var (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('input')].filter(x=>x.getAttribute('placeholder')?.indexOf('手机号')>-1)[0] ");
        await ele.ClickAsync();
        await page.Keyboard.TypeAsync(job.PhoneNum, new() { Delay = 100 });
        await Task.Delay(500, cancellation);


        await page.ClickAsync("#agreement");
        await Task.Delay(500, cancellation);

        for (var i = 0; i <= 4; i++)
        {
            (ele) = await page.ExGetElement("c_vcimg", " [...document.querySelectorAll('img')].filter(x=>x.alt?.indexOf('验证码')>-1)[0] ");
            var b64 = await page.ExGetBase64ByScreenshot($".c_vcimg");
            if (string.IsNullOrEmpty(b64))
            {
                throw new Exception("not any v-code");
            }

            var words = await GetWordsByd(b64);
            ctx.Logger.LogDebug("resolve vcode={words}", words);


            ele = await page.WaitForSelectorAsync("[placeholder=图片验证码]");
            await ele.ClickAsync();
            await page.Keyboard.TypeAsync(words, new() { Delay = 100 });
            await Task.Delay(500, cancellation);


            ele = await page.ExGetElement("c_get_code", " [...document.querySelectorAll('a')].filter(x=>x.innerText?.indexOf('获取验证码')>-1)[0] ");
            await ele.ClickAsync();
            await Task.Delay(2000, cancellation);



            await Task.Delay(1000, cancellation);
            var ierr = await page.EvaluateFunctionAsync<int>($$"""
                ()=>{
                let x = window.__get_alert_msg() || '';
                if (x.indexOf('成功') > -1) return 0;
                if (x.indexOf('失败') > -1) return 1;
                x = document.querySelector('.c_get_code')?.innerText?.toLowerCase() || '';
                if (x.indexOf('s') > -1 || x.indexOf('秒') > -1 || x.indexOf('重') > -1) return 0;
                return -1;
                }
                """);
            if (ierr == 0) return;
            ctx.Logger.LogWarn("验证码'{words}'错误,正在重试i={i}", words, i + 1);

            await Task.Delay(1200, cancellation);
            await page.ClickAsync("[placeholder=图片验证码]");
            await page.Keyboard.ClearTxtAsync(words.Length, 300);
            await Task.Delay(300, cancellation);

            await page.ClickAsync(".c_vcimg");
            await Task.Delay(2000, cancellation);
        }
        throw new Exception("fail try out");
    }

}
