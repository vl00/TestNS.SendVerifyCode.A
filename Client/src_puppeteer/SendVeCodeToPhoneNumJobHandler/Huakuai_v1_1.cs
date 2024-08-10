using Common;
using Common.JsonNet.v2;
using Common.SimpleJobs;
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

/// <summary>    

/// </summary>
public class Huakuai_v1_1
{
    public readonly string Gid = Guid.NewGuid().ToString("n");
    public readonly string JobName;
    readonly IPage page;
    readonly IFrame iframe;

    public int Simple_hk = 1;
    public int RetryCount = 5;
    public int RetryPerDelayMs = 0;
    public decimal Diff = 0m;
    public decimal Diff2 = 0.2m;
    public string BgSelector = "#c_bg";
    public string HuaKuaiSelector = "#c_hk";
    public string TuoSelector = "#c_tuo";
    public string HuaKuaiStyleLeft = ".left";

    public Func<Task<int>> CheckMove;

    public Func<string, string> JsfuncOnBeforeEach;
    public Func<string, string> JsfuncOnSetId;

    public Func<Task<byte[]>> FuncGetBytesForBgSelector;
    public Func<Task<byte[]>> FuncGetBytesForHuaKuaiSelector;

    /// <inheritdoc cref="Huakuai_v1_1"/>
    public Huakuai_v1_1(string name, IPage page)
    {
        JobName = name;
        this.page = page;
    }

    /// <inheritdoc cref="Huakuai_v1_1"/>
    public Huakuai_v1_1(string name, IFrame iframe)
    {
        JobName = name;
        this.iframe = iframe;
        this.page = iframe.GetPage();
    }

    protected Task<IElementHandle> QuerySelectorAsync(string selector)
    {
        if (iframe != null) return iframe.QuerySelectorAsync(selector);
        return page.QuerySelectorAsync(selector);
    }

    protected Task<JToken> EvaluateFunctionAsync(string script)
    {
        if (iframe != null) return iframe.EvaluateFunctionAsync(script);
        return page.EvaluateFunctionAsync(script);
    }

    protected Task<T> EvaluateFunctionAsync<T>(string script)
    {
        if (iframe != null) return iframe.EvaluateFunctionAsync<T>(script);
        return page.EvaluateFunctionAsync<T>(script);
    }

    protected Task<byte[]> ExGetBytesByScreenshot(string elementSelector, int scale = 1)
    {
        if (iframe != null) return PuppeteerUtils.ExGetBytesByScreenshot(page, iframe, elementSelector, scale);
        return page.ExGetBytesByScreenshot(elementSelector, scale);
    }

    public async Task<int> RunAsync(JobContext ctx, CancellationToken cancellation = default)
    {
        var (i, ic) = (0, RetryCount);
LB_yanzhengma:
        await default(ValueTask);


        if (JsfuncOnBeforeEach != null)
        {
            var jsfunc = JsfuncOnBeforeEach(Gid);
            await EvaluateFunctionAsync(jsfunc);
            await Task.Delay(200, cancellation);
        }


        if (JsfuncOnSetId != null)
        {
            var jsfunc = JsfuncOnSetId(Gid);
            await EvaluateFunctionAsync(jsfunc);
            await Task.Delay(200, cancellation);
        }


        // get img byte[]
        var bys_bg = FuncGetBytesForBgSelector == null ? null : await FuncGetBytesForBgSelector();
        if (bys_bg == null)
        {
            await EvaluateFunctionAsync($$"""
                ()=>{
                document.querySelector('{{HuaKuaiSelector}}').style.display = 'none';
                document.querySelector('{{BgSelector}}').style.display = 'block';
                }
                """);
            await Task.Delay(200, cancellation);
            bys_bg = await ExGetBytesByScreenshot(BgSelector);
        }
      // for test

        var bys_hk = FuncGetBytesForHuaKuaiSelector == null ? null : await FuncGetBytesForHuaKuaiSelector();
        if (bys_hk == null)
        {
            await EvaluateFunctionAsync($$"""
                ()=>{
                document.querySelector('{{HuaKuaiSelector}}').style.display = 'block';
                document.querySelector('{{BgSelector}}').style.display = 'none';
                }
                """);
            await Task.Delay(200, cancellation);
            bys_hk = await ExGetBytesByScreenshot(HuaKuaiSelector);
        }
         // for test

        await EvaluateFunctionAsync($$"""
                ()=>{
                document.querySelector('{{HuaKuaiSelector}}').style.display = 'block';
                document.querySelector('{{BgSelector}}').style.display = 'block';
                }
                """);
        await ctx.GoOnOrWaitAsync();


        var jo = await GetSlideMatchByd(bys_hk, bys_bg, Simple_hk);
        var x = (decimal)jo["target"][0];
        ctx.Logger.LogDebug("got jo=`{jo}`, x={x}", jo.ToJsonStr(), x);
          
        await DoMoveTuoX(ctx, TuoSelector, x, HuaKuaiSelector, Diff);
        await Task.Delay(1500, cancellation);

        var b = await CheckMove();
        if (b != 0)
        {
            i++;
            ctx.Logger.LogWarning("{JobName},滑块验证码操作失败,retry i={i}", JobName, i);
            if (i <= ic)
            {
                if (RetryPerDelayMs > 0) await Task.Delay(RetryPerDelayMs, cancellation);
                goto LB_yanzhengma;
            }
            return b;
        }
        ctx.Logger.LogInformation("{JobName},滑块验证码操作成功", JobName);
        await ctx.GoOnOrWaitAsync();
        return b;
    }

    async Task DoMoveTuoX(JobContext ctx, string tuoSelector, decimal x, string huakuaiSelector, decimal diff = 0m)
    {
        BoundingBox box = null;
        for (var i = 0; i < 3; i++)
        {
            var ele = await QuerySelectorAsync(tuoSelector);
            box = await ele.BoundingBoxAsync();
            if (box != null) break;
            await Task.Delay(200);
        }
        if (box == null)
        {
            throw new NullReferenceException($"'{tuoSelector}' may be hidden");
        }
        var (x0, y0) = ((box.X + box.Width / 2), (box.Y + box.Height / 2));
        await page.Mouse.MoveAsync(x0, y0);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x0 + x, y0, new() { Steps = 5 });

        var (x1, xleft) = (x0 + x, 0m);        
        for (var ii = 1; ii < 30; ii++)
        {
            var d = (await EvaluateFunctionAsync<double>($$"""
                    ()=>{
                    let s = document.querySelector('{{huakuaiSelector}}').style{{HuaKuaiStyleLeft}};
                    s = s.match(/(\-)?\d+(.\d+)?/g) || [];
                    return parseFloat(s[0], 10);
                    }
                    """));
            if (double.IsNaN(d)) throw new Exception($"huakuai's style may has no '{HuaKuaiStyleLeft}'.");
            xleft = Convert.ToDecimal(d);
            var _x = Math.Abs(xleft - x);
            if (_x <= Diff2) break;
            if (xleft >= x) await page.Mouse.MoveAsync(x1 = x1 - _x + diff, y0, new() { });
            else await page.Mouse.MoveAsync(x1 = x1 + _x + diff, y0, new() { });
        }
        ctx.Logger.LogDebug("x={x}, huakuai left={xleft}, x1={x1}", x, xleft, x1 - x0);

        await page.Mouse.UpAsync();
        await page.Mouse.ResetAsync();
    }
}
