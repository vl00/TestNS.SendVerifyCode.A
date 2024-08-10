namespace TestNS;

public class SendVcJob //: Job
{
    public readonly string Id = Guid.NewGuid().ToString("n");

    public string PhoneNum { get; set; } = "13800138003";
    public string[] Svcs { get; set; }
    public int Pc { get; set; } = 1;
    public int Tc { get; set; } = 1;
}
