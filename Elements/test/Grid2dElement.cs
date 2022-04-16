using Elements.Serialization.JSON;
using Elements.Spatial;
using System.Text.Json.Serialization;

namespace Elements
{
    /// <summary>Just a test</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.Text.Json.Serialization.JsonConverter(typeof(ElementConverter<Grid2dElement>))]
    public partial class Grid2dElement : Element
    {
        /// <summary>contains a grid</summary>
        [JsonPropertyName("Grid")]
        public Grid2d Grid { get; set; }

        [JsonConstructor]
        public Grid2dElement(Grid2d @grid, System.Guid @id, string @name)
            : base(id, name)
        {
            this.Grid = @grid;
        }
    }
}