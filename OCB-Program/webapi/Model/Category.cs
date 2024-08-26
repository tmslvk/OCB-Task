namespace webapi.Model
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Results> Results { get; set; }
    }
}
