namespace AutoBlumFarmServer.SwaggerApiResponses
{
    public class ApiObjectResponse<T>
    {
        public bool ok   { get; set; }
        public T?   data { get; set; }
    }
}