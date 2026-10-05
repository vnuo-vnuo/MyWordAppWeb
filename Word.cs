namespace MyWordAppWeb // ⚠️ 改成你的项目名
{
    public class Word
    {
        public int Id { get; set; }
        public string Spelling { get; set; } = "";
        public string Meaning { get; set; } = "";
        public DateTime NextReviewTime { get; set; } = DateTime.MinValue;
    }
}