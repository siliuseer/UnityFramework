namespace siliu.net
{
    public interface IRequest
    {
        bool IsConnected();
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="data">数据</param>
        void Send(SendEntry data);
    }
}