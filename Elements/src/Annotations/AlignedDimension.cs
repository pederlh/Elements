using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements.Annotations
{
    /// <summary>
    /// A linear dimension aligned along the line between the specified start and end.
    /// </summary>
    /// <example>
    /// [!code-csharp[Main](../../Elements/test/DimensionTests.cs?name=aligned_dimension_example)]
    /// </example>
    public class AlignedDimension : LinearDimension
    {
        /// <summary>
        /// Create an aligned dimension from JSON.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="referencePlane"></param>
        /// <param name="plane"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="displayValue"></param>
        [JsonConstructor]
        public AlignedDimension(Vector3 start,
                                Vector3 end,
                                Plane referencePlane,
                                Plane plane = null,
                                string prefix = null,
                                string suffix = null,
                                string displayValue = null) : base(start,
                                                              end,
                                                              referencePlane,
                                                              plane,
                                                              prefix,
                                                              suffix,
                                                              displayValue)
        { }

        /// <summary>
        /// Create a linear dimension where the reference line is created
        /// by offsetting from the line created between start and end 
        /// by the provided value.
        /// </summary>
        /// <param name="plane">The plane in which the dimension is created.</param>
        /// <param name="start">The start of the dimension.</param>
        /// <param name="end">The end of the dimension.</param>
        /// <param name="offset">The offset of the reference line.</param>
        public AlignedDimension(Vector3 start,
                                Vector3 end,
                                double offset = 0.0,
                                Plane plane = null) : base()
        {
            this.Plane = plane ?? new Plane(Vector3.Origin, Vector3.ZAxis);
            this.Start = start.Project(this.Plane);
            this.End = end.Project(this.Plane);
            var vRef = (this.End - this.Start).Unitized();
            var offsetDirection = vRef.Cross(this.Plane.Normal);
            this.ReferencePlane = new Plane(this.Start + offsetDirection * offset, offsetDirection);
        }
    }
}