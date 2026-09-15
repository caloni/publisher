using Publisher.Core.Models;
using Publisher.Core.Parsers;
using Publisher.Core.Utilities;
using Publisher.Core.Writers;

namespace Publisher.Core.Pipeline
{
    /// <summary>
    /// Orchestrates the full publish lifecycle as an explicit sequence of steps:
    /// <list type="number">
    ///   <item><description><see cref="LoadResources"/> — load external link/image mappings from YAML.</description></item>
    ///   <item><description><see cref="Parse"/> — parse one or more Markdown source files into <see cref="GlobalState"/>.</description></item>
    ///   <item><description><see cref="Write"/> — render the parsed state to HTML, EPUB, or passthrough output.</description></item>
    /// </list>
    /// Each method returns <c>this</c> so the steps can be chained fluently.
    /// </summary>
    /// <example>
    /// <code>
    /// new PublishPipeline(state)
    ///     .LoadResources("links.yaml")
    ///     .Parse("journal.md", "private/journal.md")
    ///     .Write(new BlogWriter(state));
    /// </code>
    /// </example>
    public class PublishPipeline
    {
        private readonly GlobalState _state;

        /// <param name="state">
        /// The shared journal state that accumulates posts and configuration
        /// as the pipeline progresses.
        /// </param>
        public PublishPipeline(GlobalState state)
        {
            _state = state;
        }

        /// <summary>
        /// Loads external resource mappings (description → URL or image path) from a YAML file
        /// and stores them in <see cref="GlobalState.Links"/>.
        /// If <paramref name="yamlPath"/> is null or empty the step is a no-op.
        /// </summary>
        public PublishPipeline LoadResources(string? yamlPath)
        {
            if (!string.IsNullOrWhiteSpace(yamlPath) && File.Exists(yamlPath))
            {
                var loaded = LinksLoader.Load(yamlPath);
                foreach (var kvp in loaded)
                    _state.Links[kvp.Key] = kvp.Value;

                var dates = LinksLoader.LoadDates(yamlPath);
                foreach (var kvp in dates)
                    _state.LinkDates[kvp.Key] = kvp.Value;
            }
            return this;
        }

        /// <summary>
        /// Parses one or more Markdown source files and registers all posts found
        /// into <see cref="GlobalState"/>.
        /// Files whose path contains the word <c>"private"</c> are tagged as private automatically.
        /// Missing files are skipped with a console warning.
        /// </summary>
        public PublishPipeline Parse(params string[] filePaths)
        {
            var parser = new CommonMarkParser(_state);
            parser.ParseFiles(filePaths);
            return this;
        }

        /// <summary>
        /// Generates output by invoking <see cref="IBlogWriter.Generate"/> on the provided writer.
        /// The writer reads from the <see cref="GlobalState"/> populated by earlier steps.
        /// </summary>
        public PublishPipeline Write(IBlogWriter writer)
        {
            writer.Generate();
            return this;
        }

        /// <summary>
        /// Generates EPUB output.
        /// </summary>
        public PublishPipeline WriteBook()
        {
            var writer = new BookWriter(_state);
            writer.Generate();
            return this;
        }
    }
}
