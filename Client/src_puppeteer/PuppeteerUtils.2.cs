using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public static partial class PuppeteerUtils
{
    public static async Task<(IElementHandle, string Class)> ExGetElement(this IPage page, string findOne)
    {
        var clss = "c_" + Guid.NewGuid().ToString("n")[..8];
        var ele = await ExGetElement(page, clss, findOne);
        return (ele, clss);
    }
    public static async Task<IElementHandle> ExGetElement(this IPage page, string clss, string findOne)
    {
        throw new Exception("不会自己写吗？？？");
    }

    public static async Task<(IElementHandle, string Class)> ExGetElement(this IFrame iframe, string findOne)
    {
        var clss = "c_" + Guid.NewGuid().ToString("n")[..8];
        var ele = await ExGetElement(iframe, clss, findOne);
        return (ele, clss);
    }
    public static async Task<IElementHandle> ExGetElement(this IFrame iframe, string clss, string findOne)
    {
        throw new Exception("不会自己写吗？？？");
    }

    public static async Task<(IElementHandle, string Class)> ExWaitForElement(this IPage page, string findOne, int? timeoutMs = null)
    {
        var clss = "c_" + Guid.NewGuid().ToString("n")[..8];
        var ele = await ExWaitForElement(page, clss, findOne, timeoutMs);
        return (ele, clss);
    }
    public static async Task<IElementHandle> ExWaitForElement(this IPage page, string clss, string findOne, int? timeoutMs = null)
    {
        throw new Exception("不会自己写吗？？？");
    }

    public static async Task<(IElementHandle, string Class)> ExWaitForElement(this IFrame iframe, string findOne, int? timeoutMs = null)
    {
        var clss = "c_" + Guid.NewGuid().ToString("n")[..8];
        var ele = await ExWaitForElement(iframe, clss, findOne, timeoutMs);
        return (ele, clss);
    }
    public static async Task<IElementHandle> ExWaitForElement(this IFrame iframe, string clss, string findOne, int? timeoutMs = null)
    {
        throw new Exception("不会自己写吗？？？");
    }
}
