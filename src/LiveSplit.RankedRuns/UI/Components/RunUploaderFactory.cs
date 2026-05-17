using System;

using LiveSplit.Model;
using LiveSplit.UI.Components;

[assembly: ComponentFactory(typeof(RunUploaderFactory))]

namespace LiveSplit.UI.Components;

public class RunUploaderFactory : IComponentFactory
{
    public string ComponentName => "RankedRuns.com";

    public string Description => "RankedRuns.com LiveSplit Plugin";

    public ComponentCategory Category => ComponentCategory.Other;

    public IComponent Create(LiveSplitState state)
    {
        return new RunUploaderComponent(state);
    }

    public string UpdateName => ComponentName;

    public string UpdateURL => null;
    public string XMLURL => null;
    public Version Version => new(0, 1, 0);
}
