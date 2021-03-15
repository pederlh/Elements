using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A unique uniqueEdge in a cell complex.
    /// </summary>
    public class UniqueEdge : EdgeBase
    {
        /// <summary>
        /// DirectedEdges that reference this Edge
        /// </summary>
        [JsonIgnore]
        public HashSet<DirectedEdge> DirectedEdges = new HashSet<DirectedEdge>();

        /// <summary>
        /// Represents a unique Edge within a CellComplex.
        /// Is not intended to be created or modified outside of the CellComplex class code.
        /// </summary>
        /// <param name="cellComplex">CellComplex that this belongs to</param>
        /// <param name="id"></param>
        /// <param name="vertexId1">One of the vertex IDs for this uniqueEdge</param>
        /// <param name="vertexId2">The other vertex ID for this uniqueEdge</param>
        internal UniqueEdge(CellComplex cellComplex, long id, long vertexId1, long vertexId2) : base(id, cellComplex)
        {
            this.SetVerticesFromIds(vertexId1, vertexId2);
        }

        [JsonConstructor]
        internal UniqueEdge(long id, long startVertexId, long endVertexId) : base(id, null)
        {
            this.SetVerticesFromIds(startVertexId, endVertexId);
        }

        /// <summary>
        /// Ensures start vertex always has a smaller ID than end vertex
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        private void SetVerticesFromIds(long id1, long id2)
        {
            if (id1 < id2)
            {
                this.StartVertexId = id1;
                this.EndVertexId = id2;
            }
            else
            {
                this.EndVertexId = id1;
                this.StartVertexId = id2;
            }
        }

        /// <summary>
        /// Get the unique hash for an Edge with list (of length 2) of its unordered vertex IDs
        /// </summary>
        /// <param name="vertexIds"></param>
        /// <returns></returns>
        public static string GetHash(List<long> vertexIds)
        {
            var sortedIds = vertexIds.ToList();
            sortedIds.Sort();
            var hash = String.Join(",", sortedIds);
            return hash;
        }

        /// <summary>
        /// Get associated DirectedEdges
        /// </summary>
        /// <returns></returns>
        public List<DirectedEdge> GetDirectedEdges()
        {
            return this.DirectedEdges.ToList();
        }

        /// <summary>
        /// Get associated Faces
        /// </summary>
        /// <returns></returns>
        public List<Face> GetFaces()
        {
            return this.GetDirectedEdges().Select(ds => ds.GetFaces()).SelectMany(x => x).Distinct().ToList();
        }

        /// <summary>
        /// Get associated Cells
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetCells()
        {
            return this.GetFaces().Select(face => face.GetCells()).SelectMany(x => x).Distinct().ToList();
        }
    }
}