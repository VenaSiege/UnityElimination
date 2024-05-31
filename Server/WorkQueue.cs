using System.Collections.Concurrent;

namespace EliminationServer {
    internal class WorkQueue {

        /// <summary>
        /// WorkQueue 是一个单例
        /// </summary>
        public static readonly WorkQueue Instance = new WorkQueue();

        /// <summary>
        /// 使用支持并发的队列
        /// </summary>
        private readonly ConcurrentQueue<Action> _queue = new();

        /// <summary>
        /// 单例模式，构造函数私有化。
        /// </summary>
        private WorkQueue() { }

        /// <summary>
        /// 将一个 Action 放入队列
        /// </summary>
        /// <param name="action"></param>
        public void Post(Action action) {
            _queue.Enqueue(action);
        }

        /// <summary>
        /// 尝试从队列中取出一个 Action。
        /// </summary>
        /// <returns>从队列中取出的 Aciton。或空，表示队列里没有 Aciton 了。</returns>
        public Action? TryDequeue() {
            if (_queue.TryDequeue(out var action)) {
                return action;
            } else {
                return null;
            }
        }
    }
}
