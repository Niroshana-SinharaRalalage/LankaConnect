using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class ReviewContent : ValueObject
{
    public string Title { get; }
    public string Content { get; }
    public List<string>? Pros { get; }
    public List<string>? Cons { get; }
    
    private ReviewContent(string title, string content, List<string>? pros, List<string>? cons)
    {
        Title = title;
        Content = content;
        Pros = pros;
        Cons = cons;
    }
    
    public static Result<ReviewContent> Create(string title, string content, List<string>? pros = null, List<string>? cons = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<ReviewContent>.Failure("Review title is required");
            
        if (string.IsNullOrWhiteSpace(content))
            return Result<ReviewContent>.Failure("Review content is required");
            
        if (content.Length > 2000)
            return Result<ReviewContent>.Failure("Review content cannot exceed 2000 characters");
            
        return Result<ReviewContent>.Success(new ReviewContent(title.Trim(), content.Trim(), pros, cons));
    }
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Title;
        yield return Content;
        
        if (Pros != null)
        {
            foreach (var pro in Pros)
                yield return pro;
        }
        
        if (Cons != null)
        {
            foreach (var con in Cons)
                yield return con;
        }
    }
}