﻿using Elements.Geometry;
using Elements.Spatial.AdaptiveGrid;
using Elements.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Vertex = Elements.Spatial.AdaptiveGrid.Vertex;

namespace Elements.Tests
{
    public class AdaptiveGridTests : ModelTest
    {
        [Fact, Trait("Category", "Examples")]
        public void AdaptiveGridPolygonKeyPointsExample()
        {
            this.Name = "Elements_Spatial_AdaptiveGrid_AdaptiveGrid";
            // <example>
            var random = new Random();

            var adaptiveGrid = new AdaptiveGrid();
            var points = new List<Vector3>()
            {
                new Vector3(-6, -4),
                new Vector3(-2, -4),
                new Vector3(3, -4),
                new Vector3(1, 4.5),
                new Vector3(6, 3),
            };
            adaptiveGrid.AddFromPolygon(Polygon.Rectangle(15, 10).TransformedPolygon(
                new Transform(new Vector3(), new Vector3(10, 0, 10))), points);

            foreach (var edge in adaptiveGrid.GetEdges())
            {
                Model.AddElement(new ModelCurve(adaptiveGrid.GetLine(edge), material: random.NextMaterial()));
            }
            // </example>
        }

        [Fact]
        public void AdaptiveGridBboxKeyPointsExample()
        {
            this.Name = "Elements_Spatial_AdaptiveGrid_AdaptiveGridBboxKeyPoints";
            // <example2>
            var random = new Random();

            var adaptiveGrid = new AdaptiveGrid();
            var points = new List<Vector3>()
            {
                new Vector3(-6, -4),
                new Vector3(-2, -4),
                new Vector3(3, -4),
                new Vector3(1, 4.5, 3),
                new Vector3(6, 3, -2),
            };
            adaptiveGrid.AddFromBbox(new BBox3(new Vector3(-7.5, -5, -3), new Vector3(10, 10, 3)), points);

            points = new List<Vector3>()
            {
                new Vector3(-6, -4, 3),
                new Vector3(-2, 0, 3),
                new Vector3(0, 4, 3),
                new Vector3(2, 6, 3)
            };
            var rectangle = Polygon.Rectangle(new Vector3(-10, -5), new Vector3(15, 10));
            adaptiveGrid.AddFromPolygon(rectangle.TransformedPolygon(new Transform(new Vector3(0, 0, 3))), points);
            points = new List<Vector3>()
            {
                new Vector3(-6, -4, 2),
                new Vector3(-2, 0, 2),
                new Vector3(0, 4, 2),
                new Vector3(2, 6, 2)
            };
            adaptiveGrid.AddFromPolygon(rectangle.TransformedPolygon(new Transform(new Vector3(0, 0, 2))), points);

            foreach (var edge in adaptiveGrid.GetEdges())
            {
                Model.AddElement(new ModelCurve(adaptiveGrid.GetLine(edge), material: random.NextMaterial()));
            }
            // </example2>
        }

        [Fact]
        public void AdaptiveGridAddVertex()
        {
            var adaptiveGrid = new AdaptiveGrid();
            var points = new List<Vector3>()
            {
                new Vector3(-6, -4),
                new Vector3(-2, -4),
                new Vector3(3, -4),
                new Vector3(1, 4.5, 3),
                new Vector3(6, 3, -2),
            };
            adaptiveGrid.AddFromPolygon(Polygon.Rectangle(15, 10), points);

            ulong id;
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(-2, -4), out id));
            var oldV = adaptiveGrid.GetVertex(id);
            var edgesBefore = oldV.Edges.Count;

            var newV = adaptiveGrid.AddVertex(new Vector3(-2, -4, 2), new ConnectVertexStrategy(oldV));
            Assert.NotNull(newV);
            Assert.False(newV.Id == 0);
            Assert.Single(newV.Edges);
            Assert.True(newV.Edges.First().StartId == id || newV.Edges.First().EndId == id);
            Assert.Equal(edgesBefore + 1, oldV.Edges.Count());
            Assert.Contains(oldV.Edges, e => e.StartId == newV.Id || e.EndId == newV.Id);
        }

        [Fact]
        public void AdaptiveGridSubtractBoxCutEdges()
        {
            var adaptiveGrid = new AdaptiveGrid();
            var polygon = Polygon.Rectangle(new Vector3(0, 0), new Vector3(10, 10));

            var points = new List<Vector3>();
            for (int i = 1; i < 10; i++)
            {
                points.Add(new Vector3(i, i, 1));
            }

            adaptiveGrid.AddFromExtrude(polygon, Vector3.ZAxis, 2, points);
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(5, 5, 1), out _));
            Assert.False(adaptiveGrid.TryGetVertexIndex(new Vector3(5, 4.9, 1), out _));

            adaptiveGrid.TryGetVertexIndex(new Vector3(5, 4, 1), out var borderId);
            var borderV = adaptiveGrid.GetVertex(borderId);
            var numEdges = borderV.Edges.Count;
            var numVertices = adaptiveGrid.GetVertices().Count;

            var o = Obstacle.FromBBox(
                new BBox3(new Vector3(4.9, 4.9, 0), new Vector3(5.1, 5.1, 2)));
            adaptiveGrid.SubtractObstacle(o);
            Assert.False(adaptiveGrid.TryGetVertexIndex(new Vector3(5, 5, 1), out _));
            Assert.False(adaptiveGrid.TryGetVertexIndex(new Vector3(5, 4.9, 1), out _));

            Assert.Equal(numEdges - 1, borderV.Edges.Count);
            //On each elevation one vertex is removed and no added
            Assert.Equal(numVertices - (3 * 1), adaptiveGrid.GetVertices().Count);
        }

        [Fact]
        public void AdaptiveGridSubtractBoxSmallDifference()
        {
            var edgesNumber = 75;
            var adaptiveGrid = new AdaptiveGrid();
            var polygon = Polygon.Rectangle(new Vector3(-41, -51), new Vector3(-39, -49));

            var points = new List<Vector3>();
            points.Add(new Vector3(-40, -49.9, 1));
            points.Add(new Vector3(-40, -49.80979, 1));

            adaptiveGrid.AddFromExtrude(polygon, Vector3.ZAxis, 2, points);

            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(-40, -49.9, 0), out _));
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(-40, -49.9, 1), out _));
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(-40, -49.9, 2), out _));
            Assert.Equal(edgesNumber, adaptiveGrid.GetEdges().Count);

            var o = Obstacle.FromBBox(
                new BBox3(new Vector3(-40.2, -50.190211303259034, 0),
                          new Vector3(-39.8, -49.809788696740966, 2)));
            adaptiveGrid.SubtractObstacle(o);

            Assert.False(adaptiveGrid.TryGetVertexIndex(new Vector3(-40, -49.9, 0), out _));
            Assert.False(adaptiveGrid.TryGetVertexIndex(new Vector3(-40, -49.9, 1), out _));
            Assert.False(adaptiveGrid.TryGetVertexIndex(new Vector3(-40, -49.9, 2), out _));
            Assert.Equal(edgesNumber - 14, adaptiveGrid.GetEdges().Count);
        }

        [Fact]
        public void AdaptiveGridSubtractMisalignedPolygon()
        {
            var boundary = new Polygon(
                new Vector3(-15.0, 49.599999999999994, 0), //TODO: Root cause of an issue, coordinates of boundary vertices are slightly misaligned
                new Vector3(-45.0, 49.6, 0),
                new Vector3(-45.0, 0, 0),
                new Vector3(-15.0, 0, 0));

            var obstacles = new List<Obstacle>
            { 
                //Small box with x-axis aligned edges to subtract
                Obstacle.FromBBox(new BBox3(new Vector3(-30.41029, 19.60979, 0),
                                            new Vector3(-29.58971, 20.39021, 0))),
                //Big box intersecting one of the edges of boundary, it should remove edges and vertices 
                Obstacle.FromBBox(new BBox3(new Vector3(-22.08622, 17.62839, 0),
                                            new Vector3(-8.57565, 38.31022, 0))),
                //Small box with x-axis aligned edges to subtract and no vertices added to grid
                Obstacle.FromBBox(new BBox3(new Vector3(-30.1, 40.79, 0),
                                            new Vector3(-29.7, 41.39021, 0)))
            };

            var points = new List<Vector3>()
            {
                new Vector3(-29.8, 40.540211303259035, 0),
                new Vector3(-30.0, 49.599999999999994, 0),
                new Vector3(-29.8, 41.540211303259035, 0),
                
                //1st BBox vertices
                new Vector3(-30.41029, 19.60979, 0),
                new Vector3(-29.58971, 19.60979, 0),
                new Vector3(-29.58971, 20.39021, 0),
                new Vector3(-30.41029, 20.39021, 0),
                
                //2nd BBox vertices inside polygon
                new Vector3(-22.08622, 17.62839, 0),
                new Vector3(-22.08622, 38.31022, 0)
            };

            var adaptiveGrid = new AdaptiveGrid();
            adaptiveGrid.AddFromPolygon(boundary, points);

            var edgesCount = adaptiveGrid.GetEdges().Count();
            var verticiesCount = adaptiveGrid.GetVertices().Count();

            adaptiveGrid.SubtractObstacles(obstacles);

            Assert.Equal(edgesCount - 9, adaptiveGrid.GetEdges().Count);
            Assert.Equal(verticiesCount - 2, adaptiveGrid.GetVertices().Count);
        }
        [Fact]
        public void AdaptiveGridLongSectionDoNowThrow()
        {
            var adaptiveGrid = new AdaptiveGrid();
            var polygon = Polygon.Rectangle(new Vector3(0, 0), new Vector3(200000, 10));

            var points = new List<Vector3>();
            points.Add(new Vector3(1, 5));
            points.Add(new Vector3(1999, 5));

            adaptiveGrid.AddFromExtrude(polygon, Vector3.ZAxis, 2, points);
        }

        [Fact]
        public void AdaptiveGridTwoAlignedSections()
        {
            var adaptiveGrid = new AdaptiveGrid();
            var polygon1 = Polygon.Rectangle(new Vector3(0, 0), new Vector3(10, 10));
            var polygon2 = Polygon.Rectangle(new Vector3(10, 2), new Vector3(20, 12));

            var points = new List<Vector3>();
            points.AddRange(polygon1.Vertices);
            points.AddRange(polygon2.Vertices);

            adaptiveGrid.AddFromExtrude(polygon1, Vector3.ZAxis, 2, points);
            adaptiveGrid.AddFromExtrude(polygon2, Vector3.ZAxis, 2, points);

            ulong id;
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(10, 2), out id));
            var vertex = adaptiveGrid.GetVertex(id);
            //Up, North, South, East, West
            Assert.Equal(5, vertex.Edges.Count);
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(10, 10), out id));
            vertex = adaptiveGrid.GetVertex(id);
            Assert.Equal(5, vertex.Edges.Count);
        }

        [Fact]
        public void AdaptiveGridDoesntAddTheSameVertex()
        {
            var adaptiveGrid = new AdaptiveGrid();
            var polygon = Polygon.Rectangle(new Vector3(0, 0), new Vector3(10, 10));
            adaptiveGrid.AddFromPolygon(polygon, new List<Vector3>());
            Assert.True(adaptiveGrid.TryGetVertexIndex(new Vector3(0, 10), out var id));
            var vertex = adaptiveGrid.GetVertex(id);
            var halfTol = adaptiveGrid.Tolerance / 2;
            var modified = vertex.Point + new Vector3(0, 0, halfTol);
            adaptiveGrid.TryGetVertexIndex(new Vector3(10, 0), out var otherId);
            var newVertex = adaptiveGrid.AddVertex(modified,
                new ConnectVertexStrategy(adaptiveGrid.GetVertex(otherId)));
            Assert.Equal(id, newVertex.Id);
            modified = vertex.Point + new Vector3(-halfTol, -halfTol, -halfTol);
            adaptiveGrid.TryGetVertexIndex(modified, out otherId, adaptiveGrid.Tolerance);
            Assert.Equal(id, otherId);
        }

        [Fact]
        public void AdaptiveGridAddVertices()
        {
            var grid = new AdaptiveGrid();

            //Just add
            var simpleLine = new Vector3[] { new Vector3(10, 0), new Vector3(20, 0) };
            var added = grid.AddVertices(simpleLine, AdaptiveGrid.VerticesInsertionMethod.Insert);
            Assert.Equal(2, added.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(10, 0), out var id0));
            Assert.True(grid.TryGetVertexIndex(new Vector3(20, 0), out var id1));
            var v0 = grid.GetVertex(id0);
            var v1 = grid.GetVertex(id1);
            Assert.Empty(v0.Edges);
            Assert.Empty(v1.Edges);

            //Add and connect
            simpleLine = new Vector3[] { new Vector3(0, 0), new Vector3(5, 0) };
            added = grid.AddVertices(simpleLine, AdaptiveGrid.VerticesInsertionMethod.Connect);
            Assert.Equal(2, added.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(0, 0), out id0));
            Assert.True(grid.TryGetVertexIndex(new Vector3(5, 0), out id1));
            v0 = grid.GetVertex(id0);
            v1 = grid.GetVertex(id1);
            Assert.Single(v0.Edges);
            Assert.Single(v1.Edges);
            Assert.Equal(v0.Edges.First().OtherVertexId(v0.Id), v1.Id);

            //Add, connect and self intersect
            var singleIntersection = new Vector3[] {
                new Vector3(0, 5),
                new Vector3(5, 5),
                new Vector3(10, 5),
                new Vector3(10, 10),
                new Vector3(8, 10),
                new Vector3(8, 2)
            };
            added = grid.AddVertices(singleIntersection, AdaptiveGrid.VerticesInsertionMethod.ConnectAndSelfIntersect);
            Assert.Equal(8, added.Count); //Single intersection point represented twice.
            Assert.True(grid.TryGetVertexIndex(new Vector3(8, 5), out var id));
            var v = grid.GetVertex(id);
            Assert.Equal(4, v.Edges.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(5, 5), out id0));
            Assert.True(grid.TryGetVertexIndex(new Vector3(10, 5), out id1));
            Assert.True(grid.TryGetVertexIndex(new Vector3(8, 10), out var id2));
            Assert.True(grid.TryGetVertexIndex(new Vector3(8, 2), out var id3));
            Assert.Contains(v.Edges, e => e.StartId == id0 || e.EndId == id0);
            Assert.Contains(v.Edges, e => e.StartId == id1 || e.EndId == id1);
            Assert.Contains(v.Edges, e => e.StartId == id2 || e.EndId == id2);
            Assert.Contains(v.Edges, e => e.StartId == id3 || e.EndId == id3);

            var douleIntersection = new Vector3[] {
                new Vector3(10, 0),
                new Vector3(20, 0),
                new Vector3(20, 5),
                new Vector3(15, 5),
                new Vector3(15, -5),
                new Vector3(12, -5),
                new Vector3(12, 5),
            };
            added = grid.AddVertices(douleIntersection, AdaptiveGrid.VerticesInsertionMethod.ConnectAndSelfIntersect);
            Assert.Equal(11, added.Count); //Two intersection points represented twice.
            Assert.True(grid.TryGetVertexIndex(new Vector3(15, 0), out id0));
            Assert.True(grid.TryGetVertexIndex(new Vector3(12, 0), out id1));
            v0 = grid.GetVertex(id0);
            v1 = grid.GetVertex(id1);
            Assert.Equal(4, v0.Edges.Count);
            Assert.Equal(4, v1.Edges.Count);
            Assert.Contains(v0.Edges, e => e.StartId == id1 || e.EndId == id1);
            Assert.True(grid.TryGetVertexIndex(new Vector3(10, 0), out id2));
            Assert.True(grid.TryGetVertexIndex(new Vector3(20, 0), out id3));
            var v2 = grid.GetVertex(id2);
            var v3 = grid.GetVertex(id3);
            Assert.Single(v2.Edges);
            Assert.Equal(2, v3.Edges.Count);
            Assert.Contains(v2.Edges, e => e.StartId == id1 || e.EndId == id1);
            Assert.Contains(v3.Edges, e => e.StartId == id0 || e.EndId == id0);

            //Add connect and cut
            simpleLine = new Vector3[] { new Vector3(2, 10), new Vector3(2, 0) };
            added = grid.AddVertices(simpleLine, AdaptiveGrid.VerticesInsertionMethod.ConnectAndCut);
            Assert.Equal(3, added.Count);
            Assert.Contains(added, v => v.Point.IsAlmostEqualTo(new Vector3(2, 10)));
            Assert.Contains(added, v => v.Point.IsAlmostEqualTo(new Vector3(2, 5)));
            Assert.Contains(added, v => v.Point.IsAlmostEqualTo(new Vector3(2, 0)));
            Assert.True(grid.TryGetVertexIndex(new Vector3(2, 5), out id0));
            Assert.True(grid.TryGetVertexIndex(new Vector3(2, 0), out id1));
            v0 = grid.GetVertex(id0);
            v1 = grid.GetVertex(id1);
            Assert.Equal(4, v0.Edges.Count);
            Assert.Equal(3, v1.Edges.Count);

            //Add cut and extend.
            grid = new AdaptiveGrid();
            grid.AddFromPolygon(Polygon.Rectangle(new Vector3(0, 0), new Vector3(10, 10)),
                                new List<Vector3> { new Vector3(5, 5) });

            var toExtend = new Vector3[] { new Vector3(1, 5), new Vector3(4, 2), new Vector3(8, 6) };
            added = grid.AddVertices(toExtend, AdaptiveGrid.VerticesInsertionMethod.ConnectCutAndExtend);
            Assert.Equal(8, added.Count);
            Assert.Equal(new Vector3(0, 6), added[0].Point);
            Assert.Equal(new Vector3(1, 5), added[1].Point);
            Assert.Equal(new Vector3(5, 1), added[2].Point);
            Assert.Equal(new Vector3(2, 0), added[3].Point);
            Assert.Equal(new Vector3(4, 2), added[4].Point);
            Assert.Equal(new Vector3(5, 3), added[5].Point);
            Assert.Equal(new Vector3(7, 5), added[6].Point);
            Assert.Equal(new Vector3(10, 8), added[7].Point);
            Assert.Equal(3, added[0].Edges.Count);
            Assert.Equal(4, added[1].Edges.Count);
            Assert.Equal(3, added[2].Edges.Count);
            Assert.Equal(3, added[3].Edges.Count);
            Assert.Equal(4, added[4].Edges.Count);
            Assert.Equal(4, added[5].Edges.Count);
            Assert.Equal(4, added[6].Edges.Count);
            Assert.Equal(3, added[7].Edges.Count);
        }

        [Fact]
        public void AdaptiveGridVertexGetEdgeOtherVertexId()
        {
            var grid = SampleGrid();
            var vertex = grid.GetVertex(2);
            Assert.Null(vertex.GetEdge(4));
            Assert.Null(vertex.GetEdge(2));

            var edge = vertex.GetEdge(1);
            Assert.True(edge.OtherVertexId(2) == 1);
            Assert.Throws<ArgumentException>(() => edge.OtherVertexId(3));
            var startVertex = grid.GetVertex(edge.StartId);
            Assert.True(startVertex.Point.IsAlmostEqualTo(new Vector3(0, 0)));
        }

        [Fact]
        public void AdaptiveGridClosestVertex()
        {
            var grid = SampleGrid();
            var closest = grid.ClosestVertex(new Vector3(5, 4));
            Assert.Equal(4u, closest.Id);
        }

        [Fact]
        public void AdaptiveGridClosestEdge()
        {
            var grid = SampleGrid();
            var edge = grid.ClosestEdge(new Vector3(9, 3), out var closest);
            Assert.True(edge.StartId == 3 || edge.StartId == 4);
            Assert.True(edge.EndId == 3 || edge.EndId == 4);
            Assert.Equal(new Vector3(8, 2), closest);
        }

        [Fact]
        public void AdaptiveGridCutEdge()
        {
            var grid = SampleGrid();
            var vertex = grid.GetVertex(1);
            var edge = vertex.GetEdge(4);
            var cut = grid.CutEdge(edge, new Vector3(0, 5));
            Assert.DoesNotContain(edge, vertex.Edges);
            Assert.DoesNotContain(edge, grid.GetEdges());
            Assert.Equal(2, cut.Edges.Count);
            Assert.Contains(cut.Edges, e => e.OtherVertexId(cut.Id) == 1);
            Assert.Contains(cut.Edges, e => e.OtherVertexId(cut.Id) == 4);
        }

        [Fact]
        public void AdaptiveGridEdgeGetVerticesGetLine()
        {
            var grid = SampleGrid();
            var vertexA = grid.GetVertex(1);
            var vertexB = grid.GetVertex(4);
            var edge = vertexA.GetEdge(4);
            var vertices = grid.GetVertices(edge);
            Assert.Equal(2, vertices.Count);
            Assert.Contains(vertices, v => v == vertexA);
            Assert.Contains(vertices, v => v == vertexB);

            var line = grid.GetLine(edge);
            Assert.True(line.Start.IsAlmostEqualTo(vertexA.Point) || line.End.IsAlmostEqualTo(vertexA.Point));
            Assert.True(line.Start.IsAlmostEqualTo(vertexB.Point) || line.End.IsAlmostEqualTo(vertexB.Point));
        }

        [Fact]
        public void AdaptiveGridRemoveVertex()
        {
            var grid = SampleGrid();
            var oldVertexCount = grid.GetVertices().Count;
            var oldEdgeCount = grid.GetEdges().Count;
            var vertex = grid.GetVertex(1);
            var edges = vertex.Edges.ToList();
            var otherVertices = edges.Select(e => grid.GetVertex(e.OtherVertexId(1)));
            grid.RemoveVertex(vertex);
            Assert.DoesNotContain(vertex, grid.GetVertices());
            Assert.Equal(oldVertexCount - 1, grid.GetVertices().Count);
            Assert.Equal(oldEdgeCount - 2, grid.GetEdges().Count);
            foreach (var e in edges)
            {
                Assert.DoesNotContain(e, grid.GetEdges());
                Assert.DoesNotContain(otherVertices, v => v.Edges.Contains(e));
            }
        }

        [Fact]
        public void AdaptiveGridAddEdgeNoCut()
        {
            var grid = SampleGrid();
            var v0 = grid.GetVertex(4);
            var v0ec = v0.Edges.Count;
            var v1 = grid.GetVertex(5);
            var v1ec = v1.Edges.Count;
            var oldVertexCount = grid.GetVertices().Count;
            var oldEdgeCount = grid.GetEdges().Count;
            var newEdges = grid.AddEdge(v0, v1, cut: false);
            Assert.Equal(oldVertexCount, grid.GetVertices().Count);
            Assert.Equal(oldEdgeCount + 1, grid.GetEdges().Count);
            Assert.Equal(v0ec + 1, v0.Edges.Count);
            Assert.Equal(v1ec + 1, v1.Edges.Count);
            Assert.Contains(newEdges.First(), v0.Edges);
            Assert.Contains(newEdges.First(), v1.Edges);
            Assert.True(newEdges.First().StartId == v0.Id);
            Assert.True(newEdges.First().EndId == v1.Id);

            oldEdgeCount = grid.GetEdges().Count;
            var otherEdge = newEdges.First();
            newEdges = grid.AddEdge(new Vector3(4, 3), new Vector3(6, 3), cut: false);
            Assert.Equal(oldVertexCount + 2, grid.GetVertices().Count);
            Assert.Equal(oldEdgeCount + 1, grid.GetEdges().Count);
            v0 = grid.GetVertex(newEdges.First().StartId);
            v1 = grid.GetVertex(newEdges.First().EndId);
            Assert.Equal(new Vector3(4, 3), v0.Point);
            Assert.Equal(new Vector3(6, 3), v1.Point);
            Assert.True(grid.GetLine(otherEdge).Intersects(grid.GetLine(newEdges.First()), out _));
        }

        [Fact]
        public void AdaptiveGridRemoveEdge()
        {
            var grid = SampleGrid();
            var v0 = grid.GetVertex(2);
            var v1 = grid.GetVertex(5);
            var v0ec = v0.Edges.Count;
            var oldVertexCount = grid.GetVertices().Count;
            var oldEdgeCount = grid.GetEdges().Count;
            var edge = v0.GetEdge(v1.Id);
            grid.RemoveEdge(edge);
            Assert.Equal(oldVertexCount - 1, grid.GetVertices().Count);
            Assert.Equal(oldEdgeCount - 1, grid.GetEdges().Count);

            Assert.DoesNotContain(edge, grid.GetEdges());
            Assert.DoesNotContain(v1, grid.GetVertices()); //v1 had only one edge.
            Assert.Contains(v0, grid.GetVertices()); //v0 had two edges
            Assert.Equal(v0ec - 1, v0.Edges.Count);
            Assert.DoesNotContain(edge, v0.Edges);
        }

        [Fact]
        public void AdaptiveGridAddCutEdge()
        {
            var grid = SampleGrid();

            //1. Just intersection.
            var v0 = grid.AddVertex(new Vector3(4, 1));
            var v1 = grid.AddVertex(new Vector3(6, 1));
            var edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Equal(2, edges.Count);
            var intersectionVertex = grid.GetVertex(edges.First().EndId);
            Assert.Equal(new Vector3(5, 1), intersectionVertex.Point);
            Assert.Equal(4, intersectionVertex.Edges.Count);

            //2. Multiply intersections
            v0 = grid.AddVertex(new Vector3(0, 4));
            v1 = grid.AddVertex(new Vector3(10, 4));
            edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Equal(3, edges.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(4, 4), out var otherId));
            Assert.True(edges.First().StartId == otherId || edges.First().EndId == otherId);
            intersectionVertex = grid.GetVertex(otherId);
            Assert.Equal(4, intersectionVertex.Edges.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(6, 4), out otherId));
            Assert.True(edges.Last().StartId == otherId || edges.Last().EndId == otherId);
            intersectionVertex = grid.GetVertex(otherId);
            Assert.Equal(4, intersectionVertex.Edges.Count);

            //3. Miss
            v0 = grid.AddVertex(new Vector3(0, 7));
            v1 = grid.AddVertex(new Vector3(10, 7));
            edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Single(edges);
            var startVertex = grid.GetVertex(edges.First().StartId);
            var endVertex = grid.GetVertex(edges.First().EndId);
            Assert.Equal(new Vector3(0, 7), startVertex.Point);
            Assert.Equal(new Vector3(10, 7), endVertex.Point);
            Assert.Single(startVertex.Edges);
            Assert.Single(endVertex.Edges);

            //4. In Plane Touch
            v0 = grid.AddVertex(new Vector3(2, 0));
            v1 = grid.AddVertex(new Vector3(2, -5));
            edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Single(edges);
            startVertex = grid.GetVertex(edges.First().StartId);
            endVertex = grid.GetVertex(edges.First().EndId);
            Assert.Equal(new Vector3(2, 0), startVertex.Point);
            Assert.Equal(3, startVertex.Edges.Count);
            Assert.Single(endVertex.Edges);

            //5. Out Plane Touch
            v0 = grid.AddVertex(new Vector3(8, 0));
            v1 = grid.AddVertex(new Vector3(8, 0, 2));
            edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Single(edges);
            startVertex = grid.GetVertex(edges.First().StartId);
            Assert.Equal(new Vector3(8, 0), startVertex.Point);
            Assert.Equal(3, startVertex.Edges.Count);
            Assert.Single(endVertex.Edges);

            //6. Inside other edge
            v0 = grid.AddVertex(new Vector3(1, 1));
            v1 = grid.AddVertex(new Vector3(3, 3));
            edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Single(edges);
            startVertex = grid.GetVertex(edges.First().StartId);
            endVertex = grid.GetVertex(edges.First().EndId);
            Assert.Equal(2, startVertex.Edges.Count);
            Assert.Equal(2, endVertex.Edges.Count);
            var otherEdge = startVertex.Edges.First(e => e.StartId != endVertex.Id && e.EndId != endVertex.Id);
            var otherVertex = grid.GetVertex(otherEdge.StartId == startVertex.Id ? otherEdge.EndId : otherEdge.StartId);
            Assert.Equal(new Vector3(0, 0), otherVertex.Point);
            otherEdge = endVertex.Edges.First(e => e.StartId != startVertex.Id && e.EndId != startVertex.Id);
            otherVertex = grid.GetVertex(otherEdge.StartId == startVertex.Id ? otherEdge.EndId : otherEdge.StartId);
            Assert.Equal(new Vector3(4, 4), otherVertex.Point);

            //7. Overlaps other edges
            v0 = grid.AddVertex(new Vector3(4, 6));
            v1 = grid.AddVertex(new Vector3(11, -1));
            edges = grid.AddEdge(v0.Id, v1.Id);
            Assert.Equal(4, edges.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(5, 5), out otherId));
            Assert.True(edges.First().StartId == otherId || edges.First().EndId == otherId);
            endVertex = grid.GetVertex(otherId);
            startVertex = grid.GetVertex(edges.First().StartId == otherId ? edges.First().EndId : edges.First().StartId);
            Assert.Single(startVertex.Edges);
            Assert.Equal(3, endVertex.Edges.Count);
            Assert.True(grid.TryGetVertexIndex(new Vector3(6, 4), out otherId));
            Assert.Contains(endVertex.Edges, e => e.StartId == otherId || e.EndId == otherId);
            otherVertex = grid.GetVertex(otherId);
            Assert.Equal(4, otherVertex.Edges.Count);
            startVertex = grid.GetVertex(edges.Last().StartId);
            endVertex = grid.GetVertex(edges.Last().EndId);
            Assert.Equal(3, startVertex.Edges.Count);
            Assert.Single(endVertex.Edges);
            Assert.Contains(startVertex.Edges, e => e.StartId == otherId || e.EndId == otherId);
            otherVertex = grid.GetVertex(otherId);
            Assert.Equal(new Vector3(6, 4), otherVertex.Point);
        }

        [Fact]
        public void AdaptiveGridAddVertexWithAngle()
        {
            var grid = new AdaptiveGrid();

            //1. Aligned with direction. 0 vertices exist.
            var s = new ConnectVertexWithAngleStrategy(new Vector3(0, 5), new Vector3(0, 1), 45);
            var startVertex = grid.AddVertex(new Vector3(0, 0), s);
            Assert.Null(s.MiddleVertex);
            Assert.Equal(new Vector3(0, 0), startVertex.Point);
            var id00 = startVertex.Id;
            Assert.Equal(new Vector3(0, 5), s.EndVertex.Point);
            var id05 = s.EndVertex.Id;

            //2. Ortho aligned with direction, 1 vertex exist.
            s = new ConnectVertexWithAngleStrategy(new Vector3(0, 0), new Vector3(0, 1), 45);
            startVertex = grid.AddVertex(new Vector3(5, 0), s);
            Assert.Null(s.MiddleVertex);
            Assert.Equal(new Vector3(5, 0), startVertex.Point);
            Assert.Equal(id00, s.EndVertex.Id);
            var id50 = startVertex.Id;

            //3. 0 degree, 2 vertices exist.
            s = new ConnectVertexWithAngleStrategy(new Vector3(5, 0), new Vector3(0, 1), 0);
            startVertex = grid.AddVertex(new Vector3(0, 5), s);
            Assert.Equal(new Vector3(5, 5), s.MiddleVertex.Point);
            Assert.Equal(new Vector3(0, 5), startVertex.Point);
            Assert.Equal(id05, startVertex.Id);
            Assert.Equal(id50, s.EndVertex.Id);

            //4. 90 degrees, 0 vertices exist.
            s = new ConnectVertexWithAngleStrategy(new Vector3(10, 0), new Vector3(0, 1), 90);
            startVertex = grid.AddVertex(new Vector3(15, 5), s);
            Assert.Equal(new Vector3(15, 0), s.MiddleVertex.Point);

            //5. 45 degrees, 1 intersection.
            s = new ConnectVertexWithAngleStrategy(new Vector3(13, -2), new Vector3(1, 0), 45);
            startVertex = grid.AddVertex(new Vector3(10, 5), s);
            Assert.Equal(new Vector3(10, 1), s.MiddleVertex.Point);
            Assert.Equal(45.0, (s.EndVertex.Point - s.MiddleVertex.Point).AngleTo(s.MiddleVertex.Point - startVertex.Point), 3);
            Assert.Contains(s.MiddleVertex.Edges, e =>
                e.StartId != s.MiddleVertex.Id && grid.GetVertex(e.StartId).Point.IsAlmostEqualTo(new Vector3(11, 0)) ||
                e.EndId != s.MiddleVertex.Id && grid.GetVertex(e.EndId).Point.IsAlmostEqualTo(new Vector3(11, 0)));

            //5. 45 degrees, tilted direction.
            s = new ConnectVertexWithAngleStrategy(new Vector3(11, 15), new Vector3(1, 1), 45);
            startVertex = grid.AddVertex(new Vector3(10, 10), s);
            Assert.Equal(new Vector3(11, 11), s.MiddleVertex.Point);
            Assert.Equal(45.0, (s.EndVertex.Point - s.MiddleVertex.Point).AngleTo(s.MiddleVertex.Point - startVertex.Point), 3);

            //6. 1 to 2 ration (26.565 degrees)
            s = new ConnectVertexWithAngleStrategy(new Vector3(15, 5), new Vector3(0, 1), 26.565);
            startVertex = grid.AddVertex(new Vector3(20, 0), s);
            Assert.Equal(new Vector3(17.5, 0), s.MiddleVertex.Point);
            var angle = (s.EndVertex.Point - s.MiddleVertex.Point).AngleTo(s.MiddleVertex.Point - startVertex.Point);
            Assert.True(angle.ApproximatelyEquals(26.565) || angle.ApproximatelyEquals(90 - 26.565));

            //7.  1 to 2 ration (26.565 degrees) full length
            s = new ConnectVertexWithAngleStrategy(new Vector3(20, 0), new Vector3(1, 0), 26.565);
            startVertex = grid.AddVertex(new Vector3(30, 5), s);
            Assert.Null(s.MiddleVertex);
            Assert.Contains(startVertex.Edges, e => e.StartId == s.EndVertex.Id || e.EndId == s.EndVertex.Id);
        }

        //          (4)
        //         /   \
        //        /     \
        //       /       \
        //      /   (5)   \
        //     /     |     \
        //    /      |      \
        //  (1)-----(2)-----(3)
        //
        private AdaptiveGrid SampleGrid()
        {
            AdaptiveGrid grid = new AdaptiveGrid();
            var strip = grid.AddVertices(new Vector3[] {
                new Vector3(0, 0), //1
                new Vector3(5, 0), //2
                new Vector3(10, 0) //3
            }, AdaptiveGrid.VerticesInsertionMethod.Connect);

            grid.AddVertex(new Vector3(5, 5), new ConnectVertexStrategy(strip[0], strip[2]), cut: false); //4
            grid.AddVertex(new Vector3(5, 2), new ConnectVertexStrategy(strip[1]), cut: false); //5
            return grid;
        }
    }
}
