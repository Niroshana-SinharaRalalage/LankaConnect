using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class SocialMediaLinksTests
{
    [Fact]
    public void Create_WithValidUrls_ShouldReturnSuccess()
    {
        var facebook = "https://facebook.com/business";
        var instagram = "https://instagram.com/business";
        var twitter = "https://twitter.com/business";
        var linkedIn = "https://linkedin.com/company/business";
        var tikTok = "https://tiktok.com/@business";
        var youTube = "https://youtube.com/channel/business";

        var result = SocialMediaLinks.Create(facebook, instagram, twitter, linkedIn, tikTok, youTube);

        Assert.True(result.IsSuccess);
        var links = result.Value;
        Assert.Equal(facebook, links.Facebook);
        Assert.Equal(instagram, links.Instagram);
        Assert.Equal(twitter, links.Twitter);
        Assert.Equal(linkedIn, links.LinkedIn);
        Assert.Equal(tikTok, links.TikTok);
        Assert.Equal(youTube, links.YouTube);
    }

    [Fact]
    public void Create_WithNoUrls_ShouldReturnSuccess()
    {
        var result = SocialMediaLinks.Create();

        Assert.True(result.IsSuccess);
        var links = result.Value;
        Assert.Null(links.Facebook);
        Assert.Null(links.Instagram);
        Assert.Null(links.Twitter);
        Assert.Null(links.LinkedIn);
        Assert.Null(links.TikTok);
        Assert.Null(links.YouTube);
    }

    [Fact]
    public void Create_WithSingleUrl_ShouldReturnSuccess()
    {
        var facebook = "https://facebook.com/business";

        var result = SocialMediaLinks.Create(facebook: facebook);

        Assert.True(result.IsSuccess);
        var links = result.Value;
        Assert.Equal(facebook, links.Facebook);
        Assert.Null(links.Instagram);
        Assert.Null(links.Twitter);
        Assert.Null(links.LinkedIn);
        Assert.Null(links.TikTok);
        Assert.Null(links.YouTube);
    }

    [Theory]
    [InlineData("https://facebook.com/business")]
    [InlineData("http://facebook.com/business")]
    [InlineData("https://www.facebook.com/business")]
    [InlineData("https://m.facebook.com/business")]
    public void Create_WithValidFacebookUrls_ShouldReturnSuccess(string facebookUrl)
    {
        var result = SocialMediaLinks.Create(facebook: facebookUrl);

        Assert.True(result.IsSuccess);
        Assert.Equal(facebookUrl, result.Value.Facebook);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://facebook.com/business")]
    [InlineData("facebook.com/business")]
    [InlineData("file://local-file")]
    public void Create_WithInvalidFacebookUrl_ShouldReturnFailure(string invalidUrl)
    {
        var result = SocialMediaLinks.Create(facebook: invalidUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid facebook URL format", result.Errors);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://instagram.com/business")]
    [InlineData("instagram.com/business")]
    public void Create_WithInvalidInstagramUrl_ShouldReturnFailure(string invalidUrl)
    {
        var result = SocialMediaLinks.Create(instagram: invalidUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid instagram URL format", result.Errors);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://twitter.com/business")]
    [InlineData("twitter.com/business")]
    public void Create_WithInvalidTwitterUrl_ShouldReturnFailure(string invalidUrl)
    {
        var result = SocialMediaLinks.Create(twitter: invalidUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid twitter URL format", result.Errors);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://linkedin.com/company/business")]
    [InlineData("linkedin.com/company/business")]
    public void Create_WithInvalidLinkedInUrl_ShouldReturnFailure(string invalidUrl)
    {
        var result = SocialMediaLinks.Create(linkedIn: invalidUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid linkedIn URL format", result.Errors);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://tiktok.com/@business")]
    [InlineData("tiktok.com/@business")]
    public void Create_WithInvalidTikTokUrl_ShouldReturnFailure(string invalidUrl)
    {
        var result = SocialMediaLinks.Create(tikTok: invalidUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid tikTok URL format", result.Errors);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://youtube.com/channel/business")]
    [InlineData("youtube.com/channel/business")]
    public void Create_WithInvalidYouTubeUrl_ShouldReturnFailure(string invalidUrl)
    {
        var result = SocialMediaLinks.Create(youTube: invalidUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid youTube URL format", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongFacebookUrl_ShouldReturnFailure()
    {
        var longUrl = "https://facebook.com/" + new string('a', 200);

        var result = SocialMediaLinks.Create(facebook: longUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("facebook URL cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthFacebookUrl_ShouldReturnSuccess()
    {
        // Create a 200-character URL that's actually valid
        var maxUrl = "https://www.facebook.com/pages/" + new string('a', 169); // Total exactly 200 chars

        var result = SocialMediaLinks.Create(facebook: maxUrl);

        Assert.True(result.IsSuccess);
        Assert.Equal(maxUrl, result.Value.Facebook);
    }

    [Fact]
    public void Create_WithTooLongInstagramUrl_ShouldReturnFailure()
    {
        var longUrl = "https://instagram.com/" + new string('a', 200);

        var result = SocialMediaLinks.Create(instagram: longUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("instagram URL cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongTwitterUrl_ShouldReturnFailure()
    {
        var longUrl = "https://twitter.com/" + new string('a', 200);

        var result = SocialMediaLinks.Create(twitter: longUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("twitter URL cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongLinkedInUrl_ShouldReturnFailure()
    {
        var longUrl = "https://linkedin.com/" + new string('a', 200);

        var result = SocialMediaLinks.Create(linkedIn: longUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("linkedIn URL cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongTikTokUrl_ShouldReturnFailure()
    {
        var longUrl = "https://tiktok.com/" + new string('a', 200);

        var result = SocialMediaLinks.Create(tikTok: longUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("tikTok URL cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongYouTubeUrl_ShouldReturnFailure()
    {
        var longUrl = "https://youtube.com/" + new string('a', 200);

        var result = SocialMediaLinks.Create(youTube: longUrl);

        Assert.True(result.IsFailure);
        Assert.Contains("youTube URL cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromUrls()
    {
        var facebook = "  https://facebook.com/business  ";
        var instagram = "  https://instagram.com/business  ";
        var twitter = "  https://twitter.com/business  ";

        var result = SocialMediaLinks.Create(facebook: facebook, instagram: instagram, twitter: twitter);

        Assert.True(result.IsSuccess);
        var links = result.Value;
        Assert.Equal("https://facebook.com/business", links.Facebook);
        Assert.Equal("https://instagram.com/business", links.Instagram);
        Assert.Equal("https://twitter.com/business", links.Twitter);
    }

    [Fact]
    public void HasAnyLinks_WithNoLinks_ShouldReturnFalse()
    {
        var links = SocialMediaLinks.Create().Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.False(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithOnlyEmptyStrings_ShouldReturnFalse()
    {
        var links = SocialMediaLinks.Create("", "", "", "", "", "").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.False(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithOnlyWhitespace_ShouldReturnFalse()
    {
        var links = SocialMediaLinks.Create("   ", "   ", "   ", "   ", "   ", "   ").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.False(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithFacebookOnly_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(facebook: "https://facebook.com/business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithInstagramOnly_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(instagram: "https://instagram.com/business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithTwitterOnly_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(twitter: "https://twitter.com/business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithLinkedInOnly_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(linkedIn: "https://linkedin.com/company/business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithTikTokOnly_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(tikTok: "https://tiktok.com/@business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithYouTubeOnly_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(youTube: "https://youtube.com/channel/business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void HasAnyLinks_WithAllLinks_ShouldReturnTrue()
    {
        var links = SocialMediaLinks.Create(
            "https://facebook.com/business",
            "https://instagram.com/business",
            "https://twitter.com/business",
            "https://linkedin.com/company/business",
            "https://tiktok.com/@business",
            "https://youtube.com/channel/business").Value;

        var hasAnyLinks = links.HasAnyLinks();

        Assert.True(hasAnyLinks);
    }

    [Fact]
    public void Equality_WithSameUrls_ShouldBeEqual()
    {
        var links1 = SocialMediaLinks.Create(
            "https://facebook.com/business",
            "https://instagram.com/business").Value;
        var links2 = SocialMediaLinks.Create(
            "https://facebook.com/business",
            "https://instagram.com/business").Value;

        Assert.Equal(links1, links2);
    }

    [Fact]
    public void Equality_WithDifferentFacebookUrls_ShouldNotBeEqual()
    {
        var links1 = SocialMediaLinks.Create(facebook: "https://facebook.com/business1").Value;
        var links2 = SocialMediaLinks.Create(facebook: "https://facebook.com/business2").Value;

        Assert.NotEqual(links1, links2);
    }

    [Fact]
    public void Equality_WithNoUrls_ShouldBeEqual()
    {
        var links1 = SocialMediaLinks.Create().Value;
        var links2 = SocialMediaLinks.Create().Value;

        Assert.Equal(links1, links2);
    }

    [Fact]
    public void GetHashCode_WithSameUrls_ShouldBeEqual()
    {
        var links1 = SocialMediaLinks.Create(facebook: "https://facebook.com/business").Value;
        var links2 = SocialMediaLinks.Create(facebook: "https://facebook.com/business").Value;

        Assert.Equal(links1.GetHashCode(), links2.GetHashCode());
    }

    [Fact]
    public void ToString_WithAllLinks_ShouldFormatCorrectly()
    {
        var links = SocialMediaLinks.Create(
            "https://facebook.com/business",
            "https://instagram.com/business",
            "https://twitter.com/business",
            "https://linkedin.com/company/business",
            "https://tiktok.com/@business",
            "https://youtube.com/channel/business").Value;

        var result = links.ToString();

        Assert.Contains("Facebook: https://facebook.com/business", result);
        Assert.Contains("Instagram: https://instagram.com/business", result);
        Assert.Contains("Twitter: https://twitter.com/business", result);
        Assert.Contains("LinkedIn: https://linkedin.com/company/business", result);
        Assert.Contains("TikTok: https://tiktok.com/@business", result);
        Assert.Contains("YouTube: https://youtube.com/channel/business", result);
        Assert.Contains("|", result); // Should contain separators
    }

    [Fact]
    public void ToString_WithSingleLink_ShouldNotContainSeparator()
    {
        var links = SocialMediaLinks.Create(facebook: "https://facebook.com/business").Value;

        var result = links.ToString();

        Assert.Equal("Facebook: https://facebook.com/business", result);
        Assert.DoesNotContain("|", result);
    }

    [Fact]
    public void ToString_WithNoLinks_ShouldReturnEmptyString()
    {
        var links = SocialMediaLinks.Create().Value;

        var result = links.ToString();

        Assert.Equal("", result);
    }

    [Fact]
    public void ToString_WithSomeLinks_ShouldOnlyIncludeExistingOnes()
    {
        var links = SocialMediaLinks.Create(
            facebook: "https://facebook.com/business",
            twitter: "https://twitter.com/business").Value;

        var result = links.ToString();

        Assert.Contains("Facebook:", result);
        Assert.Contains("Twitter:", result);
        Assert.DoesNotContain("Instagram:", result);
        Assert.DoesNotContain("LinkedIn:", result);
        Assert.DoesNotContain("TikTok:", result);
        Assert.DoesNotContain("YouTube:", result);
    }

    [Theory]
    [InlineData("https://facebook.com/page.with.dots")]
    [InlineData("https://instagram.com/user_with_underscores")]
    [InlineData("https://twitter.com/user-with-dashes")]
    [InlineData("https://linkedin.com/company/company-name-123")]
    [InlineData("https://tiktok.com/@user.name_123")]
    [InlineData("https://youtube.com/c/ChannelName")]
    public void Create_WithValidVariousUrlFormats_ShouldReturnSuccess(string url)
    {
        var result = SocialMediaLinks.Create(facebook: url);

        Assert.True(result.IsSuccess);
        Assert.Equal(url, result.Value.Facebook);
    }

    [Fact]
    public void Create_WithRealWorldUrls_ShouldReturnSuccess()
    {
        var realUrls = new
        {
            Facebook = "https://www.facebook.com/LankaConnect",
            Instagram = "https://www.instagram.com/lankaconnect_official/",
            Twitter = "https://twitter.com/LankaConnect",
            LinkedIn = "https://www.linkedin.com/company/lankaconnect/",
            TikTok = "https://www.tiktok.com/@lankaconnect",
            YouTube = "https://www.youtube.com/c/LankaConnect"
        };

        var result = SocialMediaLinks.Create(
            realUrls.Facebook,
            realUrls.Instagram,
            realUrls.Twitter,
            realUrls.LinkedIn,
            realUrls.TikTok,
            realUrls.YouTube);

        Assert.True(result.IsSuccess);
        var links = result.Value;
        Assert.Equal(realUrls.Facebook, links.Facebook);
        Assert.Equal(realUrls.Instagram, links.Instagram);
        Assert.Equal(realUrls.Twitter, links.Twitter);
        Assert.Equal(realUrls.LinkedIn, links.LinkedIn);
        Assert.Equal(realUrls.TikTok, links.TikTok);
        Assert.Equal(realUrls.YouTube, links.YouTube);
        Assert.True(links.HasAnyLinks());
    }
}