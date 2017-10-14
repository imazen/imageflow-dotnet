using System.Threading;
using System.Threading.Tasks;

namespace Imageflow.Fluent
{

  
    public class BuildEndpoint : BuildItemBase
    {
        internal BuildEndpoint(FluentBuildJob builder,object nodeData, BuildNode inputNode, BuildNode canvasNode) : base(builder, nodeData, inputNode,
            canvasNode){}


        public Task<BuildJobResult> FinishAsync() => Builder.FinishAsync();

        public Task<BuildJobResult> FinishAsync(CancellationToken cancellationToken) =>
            Builder.FinishAsync(cancellationToken);
        
    }
}
