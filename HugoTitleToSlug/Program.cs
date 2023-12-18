using System.Text;
using System.Text.RegularExpressions;
using CsdnArticleExtract.Utilities;

namespace HugoTitleToSlug
{
    internal class Program
    {
        static Regex frontMatterRegex = new(@"^---(.|\n|\r)*?---");
        static Regex frontMatterTitleRegex = new(@"^title: (?<content>.*)(\r|\n)+", RegexOptions.Multiline);
        static Regex frontMatterSlugRegex = new(@"^slug: (?<content>.*)(\r|\n)+", RegexOptions.Multiline);

        static void Main(string[] args)
        {
            string dir = ConsoleUtils.InputUntil("输入存储博客的目录: ", v => Directory.Exists(v));
            string[] blogFiles = Directory.GetFiles(dir, "*.md", SearchOption.AllDirectories);

            foreach (var blogFile in blogFiles)
            {
                string markdown = File.ReadAllText(blogFile);
                string newMarkdown = InsertSlugToBlog(markdown, out string? generatedSlug);

                if (markdown == newMarkdown)
                {
                    ConsoleUtils.Log($"No Change for: {Path.GetFileName(blogFile)}");
                }
                else
                {
                    if (generatedSlug is string)
                    {
                        File.WriteAllText(blogFile, newMarkdown);
                        ConsoleUtils.Log($"Slug for {Path.GetFileName(blogFile)}: {generatedSlug}");
                    }
                    else
                    {
                        ConsoleUtils.Warn($"No Slug for {Path.GetFileName(blogFile)}");
                    }
                }
            }
        }

        static string? GetSlugFromFrontMatter(string frontMatter)
        {
            if (frontMatterTitleRegex.Match(frontMatter) is Match match && match.Success)
            {
                string title = match.Groups["content"].Value;

                title = title.Trim();
                title = title.Trim('"');
                title = title.Trim('\'');

                return GetSlugFromTitle(title);
            }

            return null;
        }

        static string InsertSlugToFrontMatter(string frontMatter, out string? slug)
        {
            var frontMatterContent = frontMatter
                .Trim('-')
                .Trim();

            slug = null;
            if (GetSlugFromFrontMatter(frontMatterContent) is string generatedSlug)
            {
                bool replacedExist = false;
                frontMatterContent = frontMatterSlugRegex.Replace(frontMatterContent, match =>
                {
                    replacedExist = true;
                    return $"slug: '{generatedSlug}'{Environment.NewLine}";
                });

                if (!replacedExist)
                {
                    frontMatterContent = frontMatterTitleRegex.Replace(frontMatterContent, match => $"{match.Value.Trim()}{Environment.NewLine}slug: '{generatedSlug}'{Environment.NewLine}");
                }

                slug = generatedSlug;
            }

            return
                $"""
                ---
                {frontMatterContent}
                ---
                """;
        }

        static string InsertSlugToBlog(string markdown, out string? slug)
        {
            string? generatedSlug = null;
            string newBlogContent = frontMatterRegex.Replace(markdown, match =>
            {
                string newFrontMatter = InsertSlugToFrontMatter(match.Value, out string? generatedSlugInner);
                if (generatedSlugInner is string)
                    generatedSlug = generatedSlugInner;

                return newFrontMatter;
            });

            slug = generatedSlug;
            return newBlogContent;
        }

        static Dictionary<string, string> codingLanguages = new()
        {
            { "C#", "CSharp" },
            { "c#", "CSharp" },

            { "C++", "CPP" },
            { "c++", "CPP" },

            { ".NET", "DotNet" },
            { ".net", "dotnet" }
        };

        static string ReplaceCodingLanguages(string text)
        {
            foreach (var lang in codingLanguages)
            {
                text = Regex.Replace(text, Regex.Escape(lang.Key), match =>
                {
                    int index = match.Index;
                    int nextCharIndex = match.Index + match.Length;

                    if (index != 0 && char.IsAsciiLetter(text[index - 1]))
                        return match.Value;
                    if (nextCharIndex < text.Length && char.IsAsciiLetter(text[nextCharIndex]))
                        return match.Value;

                    return lang.Value;
                });
            }

            return text;
        }

        static string ReplaceSpecialChars(string text)
        {
            StringBuilder sb = new StringBuilder(text);
            sb.Replace('\\', ',');
            sb.Replace('/', ',');
            sb.Replace(':', ' ');
            sb.Replace('*', ' ');
            sb.Replace('?', ',');
            sb.Replace('!', ',');
            sb.Replace('"', ' ');
            sb.Replace('\'', ' ');
            sb.Replace('<', '(');
            sb.Replace('>', ')');
            sb.Replace('|', ' ');

            return sb.ToString();
        }

        static string GetSlugFromTitle(string title)
        {
            string slug = title;
            slug = Regex.Replace(slug, @"\s", "");
            slug = ReplaceCodingLanguages(slug);
            slug = ReplaceSpecialChars(slug);

            return slug;
        }
    }
}
