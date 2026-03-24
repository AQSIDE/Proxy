namespace ProxyServer.Routing.Default;

public abstract class MultipleChoiceHandler : RoutingHandler
{
    protected virtual string EndEarlyText => "-1";
    protected virtual bool CanEndEarly => true;
    protected virtual int MaxIteration => 99;
    protected virtual bool NeedMaxIteration => false;
    protected virtual string ChoiceText => ">>> ";
    protected bool IsEarly { get; private set; }
    
    public override async Task Handle(UserContext ctx)
    {
        while (true)
        {
            WriteHeader(ctx);

            IsEarly = false;
            var selected = new List<string>();
            var iteration = 0;

            while (true)
            {
                Console.Write(ChoiceText);
                var text = Console.ReadLine();
                
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (CanEndEarly && text == EndEarlyText)
                    {
                        IsEarly = await OnEarly(selected, text, ctx);
                        if (IsEarly) break;
                        else continue;
                    }
                    
                    selected.Add(text);
                    iteration++;
                }
                
                if (NeedMaxIteration && iteration >= MaxIteration) break;
            }
            
            if (!await OnResult(selected, ctx)) break;
        }
    }

    protected abstract Task<bool> OnResult(List<string> selected, UserContext ctx);
    protected abstract Task<bool> OnEarly(List<string> selected, string lastText, UserContext ctx);
    protected abstract void WriteHeader(UserContext ctx);
}