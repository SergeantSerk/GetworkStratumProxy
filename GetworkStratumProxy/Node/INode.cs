using System;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Node
{
    internal interface INode : IDisposable
    {
        public void Start();
        public void Stop();

        public string[] GetJob();
        public Task<bool> SendSolutionAsync(string[] solution);
    }
}
