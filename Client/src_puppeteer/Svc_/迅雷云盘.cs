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

partial class Svc_迅雷云盘(IServiceProvider serviceProvider)
{
    readonly BrowserService _browserService = serviceProvider.GetService<BrowserService>();

    public async Task OnHandle(SendVeCodeToPhoneNumJob job, JobContext ctx, CancellationToken cancellation)
    {
        var url = "https://pan.xunlei.com/login/?filter=done&path=%2F";

        using var page = await _browserService.NewPageAndInit();
        using var _pageRequestInterceptor = await PageRequestInterceptor.Use(page);

        await page.GoToAsync(url);

        await page.WaitForFunctionAsync("""
            ()=>{
            var iframe = Array.from(document.querySelectorAll('iframe')).filter(s=>(s.src||'').indexOf('i.xunlei.com')>-1)[0];
            if (iframe) iframe.name = 'ifr_vcode';
            return iframe;
            }
            """);
        var frame = await page.ExGetIframeAsync("[name=ifr_vcode]");
        await Task.Delay(1000, cancellation);
        var ele = await frame.WaitForSelectorAsync(".xluweb-login-tabs");

        await frame.EvaluateFunctionAsync("""
            ()=>{
            var span = Array.from(document.querySelectorAll('span')).filter(s=>s.innerText=='手机验证登录')[0];
            span.id = 'span_vcode';
            }
            """);
        ele = await frame.QuerySelectorAsync("#span_vcode");
        await ele.ClickAsync();
        await Task.Delay(200, cancellation);

        await frame.ClickAsync("[placeholder=请输入手机号]");
        await page.Keyboard.TypeAsync(job.PhoneNum, new() { Delay = 100 });

        await frame.ClickAsync(".xlubase-login-content__code");
        await Task.Delay(1500, cancellation);

        var ierr = await CheckOk();
        if (ierr == 0) return;
        else
        {
            throw new Exception("直接获取vcode失败");
        }

        async Task<int> CheckOk()
        {
            var ok = await frame.EvaluateFunctionAsync<int>("""
                () => {
                var a = document.querySelectorAll('.xlubase-login-content__code');
                if (!a.length) return false;
                var str = a[0].innerText?.toLowerCase() || '';
                if (str.indexOf('s') > -1 || str.indexOf('秒') > -1) return 0;
                return 1;
                }
                """);
            return ok;
        }
    }

}

