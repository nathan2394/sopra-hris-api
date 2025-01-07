namespace sopra_hris_api.Responses
{
    public class Response<T>
    {
        public T Data { get; set; }
        public Response() { }
        public Response(T data)
        {
            Data = data;
        }
    }
}


