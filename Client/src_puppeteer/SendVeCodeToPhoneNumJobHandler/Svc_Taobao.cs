using Common;
using Common.JsonNet.v2;
using Common.SimpleJobs;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TestNs.PuppeteerSharp.Jobs;

partial class Svc_Taobao(IServiceProvider serviceProvider)
{
    readonly BrowserService _browserService = serviceProvider.GetService<BrowserService>();

    public async Task OnHandle(SendVeCodeToPhoneNumJob job, JobContext ctx, CancellationToken cancellation)
    {
        using var page = await _browserService.NewPageAndInit();
		await page.SetViewportAsync(new() { Width = 383, Height = 800 });
		await page.EvaluateFunctionOnNewDocumentAsync("()=>localStorage.clear()");
		await page.DeleteCookieAsync(await page.GetCookiesAsync("https://login.m.taobao.com"));	
		await page.DeleteCookieAsync(await page.GetCookiesAsync("https://main.m.tmall.com"));	
        using var _pageRequestInterceptor = await PageRequestInterceptor.Use(page);

        var url = "https://login.m.taobao.com/login.htm?ttid=h5%40iframe&redirectURL=%2F%2Fh5.m.taobao.com%2Fother%2Floginend.html%3Forigin%3Dhttps%253A%252F%252Fmain.m.taobao.com";
        await page.GoToAsync(url);
        await Task.Delay(1000, cancellation);
		
		var (ele, clss) = await page.ExWaitForElement(" [...document.querySelectorAll('input')].filter(x=>x.getAttribute('placeholder')?.indexOf('手机号')>-1)[0] ");
        await ele.ClickAsync();
		await page.Keyboard.TypeAsync(job.PhoneNum, new() { Delay = 100 });
        await Task.Delay(200, cancellation);
		await page.EvaluateFunctionAsync("""
			()=>{
				let ele = [...document.querySelectorAll('span')].filter(x=>x.innerText?.indexOf('已阅读')>-1)[0].parentElement.childNodes[0];
				ele.click();
			}
			""");
        await Task.Delay(200, cancellation);

		(ele, _) = await page.ExGetElement(" [...document.querySelectorAll('button')].filter(x=>x.innerText?.indexOf('登录')>-1)[0] ");
        await ele.ClickAsync();
        await Task.Delay(2000, cancellation);

		var ierr = await CheckOk();
        if (ierr == 0) return;
		throw new Exception("直接获取vcode失败");


        async Task<int> CheckOk()
        {
            var ok = -1;
            for (var i = 0; i < 3; i++)
            {
                ok = await page.EvaluateFunctionAsync<int>($$"""
                    () => {
                    var a = [...document.querySelectorAll('*')].filter(x=>x.innerText?.indexOf('已发送')>-1 && x.innerText?.indexOf('请耐心')>-1)
                        .filter((x,_,arr)=>arr.findIndex(x2=>x2.parentElement==x)==-1)
                        .filter(x=>x.nodeName.toLowerCase()!="script")
                        .filter(x=>{
                            let s = x?.innerText?.toLowerCase() || '';
                            return (s.indexOf('s') > -1 || s.indexOf('秒') > -1 || s.indexOf('重') > -1);
                        })[0];
                        if (a) return 0;
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
