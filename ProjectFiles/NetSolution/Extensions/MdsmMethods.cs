using Cca.Extensions.Common;
using System;
using UAManagedCore;

namespace NetZero.Extensions
{
    public class MdsmMethodsFactory : IBehaviourFactory
    {
        #region Public Methods

        public IBehaviour MakeBehaviour(IUANode node)
        {
            return new MdsmMethods(node);
        }

        #endregion Public Methods
    }

    [Behaviour(NetZero.Extensions.OptixHelpers.CcaPlugAndProduceGuid)]
    internal class MdsmMethods : IExecutableBehaviour
    {
        #region Private Fields

        private readonly IUAObject _node;

        #endregion Private Fields

        #region Public Constructors

        public MdsmMethods(IUANode node)
        {
            _node = (IUAObject)node;
        }

        #endregion Public Constructors

        #region Public Methods

        public bool ExecuteMethod(IUANode targetNode, string methodName, object[] inputArgs, out object[] outputArgs)
        {
            //Log.Info($"{Log.Node(targetNode)}: executing {methodName}");

            outputArgs = new object[0];

            if (methodName.StartsWith("Cause"))
            {
                var executable = targetNode.Get(methodName).GetVariable("Executable");
                if (executable is { } && executable.Value.Value.ToBool())
                {
                    Log.Info($"{Log.Node(targetNode)}.{methodName}.Executable is true");
                    // code to write to the controller backing tag
                    Log.Info($"{Log.Node(targetNode)}: executing {methodName}");
                    var commitTrigger = targetNode.Get(methodName).GetVariable("CommitTrigger");
                    commitTrigger.DataValue = new DataValue(new UAValue(DateTime.UtcNow.Millisecond), 192, DateTime.UtcNow);
                    return true;
                }
                else
                {
                    Log.Info($"{Log.Node(targetNode)}.{methodName}.Executable is false");
                    return false;
                }
            }
            return false;
        }

        public void Start()
        {
            //throw new NotImplementedException();
        }

        public void Stop()
        {
            //throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}