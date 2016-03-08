namespace Feri.MS.Http.Template
{
    public interface ITemplate
    {
        bool AddAction(string name, string pattern, string data);
        bool DeleteAction(string name);
        byte[] GetByte();
        string GetString();
        void LoadString(string data);
        void LoadString(byte[] data);
        void ProcessAction();
        bool UpdateAction(string name, string data);
        bool UpdateAction(HttpRequest rquest, HttpResponse response);
    }
}