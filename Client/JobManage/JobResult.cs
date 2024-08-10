namespace Common.SimpleJobs;

public class JobResult
{
    public bool Success { get; set; }
    public object Data { get; set; }
    public string Msg { get; set; }
}
