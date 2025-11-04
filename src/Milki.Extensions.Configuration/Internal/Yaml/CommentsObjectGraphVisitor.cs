using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace Milki.Extensions.Configuration.Internal.Yaml
{
    internal class CommentsObjectGraphVisitor : ChainedObjectGraphVisitor
    {
        public CommentsObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context, ObjectSerializer serializer)
        {
            if (value is CommentsObjectDescriptor { Comment: not null } commentsDescriptor)
            {
                context.Emit(new YamlDotNet.Core.Events.Comment(commentsDescriptor.Comment, false));
            }

            return base.EnterMapping(key, value, context, serializer);
        }
    }
}