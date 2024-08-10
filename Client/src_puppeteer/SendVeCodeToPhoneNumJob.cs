using Common.SimpleJobs;

namespace TestNs.PuppeteerSharp.Jobs;

public class SendVeCodeToPhoneNumJob : Job
{
    public string GroupJobId { get; set; }

    public string PhoneNum { get; set; }

    public string Svc { get; set; }
}
