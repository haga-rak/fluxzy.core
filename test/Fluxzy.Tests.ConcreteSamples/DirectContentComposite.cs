namespace Fluxzy.Tests.ConcreteSamples
{
    /// <summary>
    /// Test case corresponding to a combination of multiple rule 
    /// </summary>
    public class DirectContentComposite : BaseComposite
    {
        public override string Description { get; } = "Mock a static response to any request comming to the proxy";

        public override string LongDescription { get; } = "";

        public override string Configuration { get; } = """
rules:
  - filter: 
      typeKind: AnyFilter 
    actions: 
      - typeKind: mockedResponseAction 
        response:
          statusCode: 200 
          bodyContent: 
            origin: FromString 
            text: 'Hello is it me you lookie for'
"""; 
    }
}