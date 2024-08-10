using System;

namespace Common.SimpleJobs;

public class Job
{
    public string Id { get; set; }

    public virtual Type HandleType { get; set; }
}
