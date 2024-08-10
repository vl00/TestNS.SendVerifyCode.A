using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public static partial class PuppeteerUtils
{
    public static void ExSetUserDataDir(this LaunchOptions launchOptions, string path)
    {
        launchOptions.UserDataDir = Environment.ExpandEnvironmentVariables(path).TrimEnd('\\').TrimEnd('/');
    }

    public static async Task TryInjectJsLib(this IPage s, string libName, string jsfuncCheck, string libFile = null, string libUrl = null,
        int waitMs = 1000 * 3)
    {
        while (s?.IsClosed == false)
        {
            try
            {
                try
                {
                    await s.WaitForFunctionAsync(jsfuncCheck, new() { Timeout = waitMs }, null); // jsfuncCheck must return true
                    Debug.WriteLine($"find or inject {libName} ok");
                    return;
                }
                catch
                {
                    Debug.WriteLine($"not found {libName}");
                }

                if (!string.IsNullOrEmpty(libFile) && File.Exists(libFile))
                {
                    var js = await File.ReadAllTextAsync(libFile);
                    await s.EvaluateExpressionAsync(js);
                }
                else if (!string.IsNullOrEmpty(libUrl))
                {
                    await s.EvaluateExpressionAsync($"""
                         var s=document.createElement('script');
                         s.src = '{libUrl}';
                         document.body.appendChild(s); 
                         """);
                }
                else
                {
                    throw new ArgumentNullException("need libFile or libUrl");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"inject {libName} fail: {ex.Message}");
            }
            await Task.Delay(waitMs);
        }
    }

    public static async Task TryInjectJsLib(this IFrame s, string libName, string jsfuncCheck, string libFile = null, string libUrl = null,
        int waitMs = 1000 * 3)
    {
        for (var page = s?.GetPage(); page?.IsClosed == false;)
        {
            try
            {
                try
                {
                    await s.WaitForFunctionAsync(jsfuncCheck, new() { Timeout = waitMs }, null); // jsfuncCheck must return true
                    Debug.WriteLine($"find or inject {libName} ok");
                    return;
                }
                catch
                {
                    Debug.WriteLine($"not found {libName}");
                }

                if (!string.IsNullOrEmpty(libFile) && File.Exists(libFile))
                {
                    var js = await File.ReadAllTextAsync(libFile);
                    await s.EvaluateExpressionAsync(js);
                }
                else if (!string.IsNullOrEmpty(libUrl))
                {
                    await s.EvaluateExpressionAsync($"""
                         var s=document.createElement('script');
                         s.src = '{libUrl}';
                         document.body.appendChild(s); 
                         """);
                }
                else
                {
                    throw new ArgumentNullException("need libFile or libUrl");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"inject {libName} fail: {ex.Message}");
            }
            await Task.Delay(waitMs);
        }
    }

    public static Task TryInjectJQuery(this IPage page, int waitMs = 1000 * 3)
    {
        return TryInjectJsLib(page, "jQuery", "()=>jQuery.prototype.jquery!=null", "jquery-3.6.4.min.js", "https://unpkg.com/jquery@3.6.4/dist/jquery.min.js", waitMs);
    }

    public static async Task ExScrollIntoViewAsync(this IPage page, string selector)
    {        
        await page.EvaluateFunctionAsync($"()=>document.querySelector('{selector}').scrollIntoView()");
    }

    public static async Task ClearTxtAsync(this IKeyboard keyboard, int txtLength, int? delay = null)
    {        
        for (var i = 0; i < txtLength; i++)
        {
            if (i == 0) await keyboard.PressAsync("End");
            await keyboard.PressAsync("Backspace", new() { Delay = delay });
        }
    }

    public static async Task<IFrame> ExGetIframeAsync(this IPage page, string selector)
    {
        throw new Exception("不会自己写吗？");
    }

    public static IPage GetPage(this IFrame iframe)
    {
        return ReflectionUtil.GetValue(iframe, typeof(Frame), "FrameManager")?.GetValue(nameof(Page)) as IPage;
    }

	// missing
    public static Task<IJSHandle> WaitForFunctionAsync(this IFrame iframe, string script)
    {
        return iframe.WaitForFunctionAsync(script, new() { });
    }

    
}

public static partial class PuppeteerUtils
{
    public static async Task<byte[]> ExGetImageBytesByUrl(this IPage page, string imgUrl, CancellationToken cancellation = default)
    {
        await page.GoToAsync(imgUrl);
        await Task.Delay(1200, cancellation);

        await page.EvaluateExpressionAsync(File.ReadAllText("img_to_base64url.js"));
        var b64 = await page.EvaluateFunctionAsync<string>("""
             ()=> { var imgs = document.getElementsByTagName('img');
             if (imgs.length!=1 || !imgs[0]) return "";
             var b64=getImageBase64DataURL(imgs[0]);
             return getBase64FromDataUrl(b64); }
             """);

        return string.IsNullOrEmpty(b64) ? null : Convert.FromBase64String(b64);
    }

    public static async Task<byte[]> ExGetImageBytesByUrl(this IBrowser browser, string imgUrl, CancellationToken cancellation = default)
    {
        using var pg = await browser.NewPageAsync();
        return await ExGetImageBytesByUrl(pg, imgUrl, cancellation);
    }

    public static async Task<string> ExGetBase64ByScreenshot(this IPage page, string elementSelector, int scale = 1)
    {
        var img = await page.QuerySelectorAsync(elementSelector);
        var imgBox = await img.BoundingBoxAsync();
        var b64 = await page.ScreenshotBase64Async(new()
        {
            Clip = new() { X = imgBox.X, Y = imgBox.Y, Width = imgBox.Width, Height = imgBox.Height, Scale = scale }
        });
        return b64;
    }


    public static async Task<byte[]> ExGetBytesByScreenshot(this IPage page, string elementSelector, int scale = 1)
    {
        var img = await page.QuerySelectorAsync(elementSelector);
        var imgBox = await img.BoundingBoxAsync();
        var bys = await page.ScreenshotDataAsync(new()
        {
            Clip = new() { X = imgBox.X, Y = imgBox.Y, Width = imgBox.Width, Height = imgBox.Height, Scale = scale }
        });
        return bys;
    }

    public static async Task<string> ExGetBase64ByScreenshot(IPage page, IFrame iframe, string elementSelector, int scale = 1)
    {
        var img = await iframe.QuerySelectorAsync(elementSelector);
        var imgBox = await img.BoundingBoxAsync(); 
        var b64 = await page.ScreenshotBase64Async(new()
        {
            Clip = new() { X = imgBox.X, Y = imgBox.Y, Width = imgBox.Width, Height = imgBox.Height, Scale = scale }
        });
        return b64;
    }

    public static Task<string> ExGetBase64ByScreenshot(this IFrame iframe, string elementSelector, int scale = 1) => ExGetBase64ByScreenshot(GetPage(iframe), iframe, elementSelector, scale);


    public static async Task<byte[]> ExGetBytesByScreenshot(IPage page, IFrame iframe, string elementSelector, int scale = 1)
    {
        var img = await iframe.QuerySelectorAsync(elementSelector);
        var imgBox = await img.BoundingBoxAsync(); 
        var bys = await page.ScreenshotDataAsync(new()
        {
            Clip = new() { X = imgBox.X, Y = imgBox.Y, Width = imgBox.Width, Height = imgBox.Height, Scale = scale }
        });
        return bys;
    }


    public static Task<byte[]> ExGetBytesByScreenshot(this IFrame iframe, string elementSelector, int scale = 1) => ExGetBytesByScreenshot(GetPage(iframe), iframe, elementSelector, scale);
}
