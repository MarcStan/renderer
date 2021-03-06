using Microsoft.Xna.Framework.Graphics;
using System;

namespace Renderer.Meshes
{
    /// <summary>
    /// This dynamic mesh implementation uses a circular queue to append vertex data to its buffer instead of replacing it.
    /// Is is very well suited for meshes that get drawn and updated MULTIPLE times PER frame in a jagged-kind of fashion (Update1, Draw1, Update2, Draw2, ..).
    /// Monogame/Xna recommends to just use DrawUserPrimitives, this class is able to do the copy in a more efficient way than if you manually created a vertex buffer, SetData into it, and then drew from that.
    /// Fix for common issue: http://blogs.msdn.com/b/shawnhar/archive/2010/07/07/setdataoptions-nooverwrite-versus-discard.aspx
    /// </summary>
    internal sealed class CircularBufferMesh : DynamicMesh
    {
        private readonly VertexDeclaration _declaration;
        private readonly GraphicsDevice _device;
        private readonly Type _vertexType;

        private int _numPrimitives;
        private int _numVertices;
        private PrimitiveType _type;
        private DynamicVertexBuffer _vertexBuffer;
        private int _internalVerticesStartIndex;

        private int _userStartIndex;
        private int _userPrimitiveCount;

        public CircularBufferMesh(GraphicsDevice device, Type vertexType, VertexDeclaration decl, PrimitiveType type)
        {
            _device = device;
            _vertexType = vertexType;
            _declaration = decl;
            _type = type;
        }

        public override int Primitives => _numPrimitives;

        public override PrimitiveType Type => _type;

        public override int Vertices => _numVertices;

        private int VerticesEndIndex => _internalVerticesStartIndex + _numVertices;

        public override int PrimitiveRange => _userPrimitiveCount;

        public override void Update<T>(T[] vertices)
        {
            Update(vertices, _type);
        }

        public override void Update<T>(T[] vertices, PrimitiveType type)
        {
            if (typeof(T) != _vertexType)
            {
                throw new ArgumentException();
            }

            _type = type;
            Append(vertices);
            _userStartIndex = 0;
            _userPrimitiveCount = _numPrimitives;
        }

        public override void UpdatePrimitiveRange(int startIndex, int primitiveCount)
        {
            if (startIndex < 0 || (startIndex > 0 && startIndex >= Primitives))
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (primitiveCount < 0 || startIndex + primitiveCount > Primitives)
                throw new ArgumentOutOfRangeException(nameof(primitiveCount));

            _userStartIndex = startIndex;
            _userPrimitiveCount = primitiveCount;
        }

        public override void Attach()
        {
            _device.SetVertexBuffer(_vertexBuffer);
        }

        public override void Detach()
        {
            _device.SetVertexBuffer(null);
        }

        public override void Draw()
        {
            if (_vertexBuffer != null)
                _device.DrawPrimitives(_type, _internalVerticesStartIndex + _userStartIndex, _userPrimitiveCount);
        }

        private void Append<T>(T[] vertices) where T : struct
        {
            int numPrimitives = CalcPrimitives(_type, vertices.Length);

            int vertexLength = vertices.Length;
            const int growSize = 1024;
            const int clearSize = 10 * growSize;

            if (_vertexBuffer == null || VerticesEndIndex + vertexLength > _vertexBuffer.VertexCount)
            {
                int minVertexSize = VerticesEndIndex + vertexLength;
                int newVertexSize = (int)(Math.Ceiling(1.0 * minVertexSize / growSize) * growSize);

                _vertexBuffer = new DynamicVertexBuffer(_device, _declaration, newVertexSize, BufferUsage.WriteOnly);
                _internalVerticesStartIndex = 0;
                _numVertices = 0;
            }

            if (VerticesEndIndex + vertexLength > clearSize)
            {
                // Begin at start...
                _vertexBuffer.SetData(vertices, 0, vertexLength, SetDataOptions.Discard);
                _internalVerticesStartIndex = 0;
                _numVertices = vertexLength;
            }
            else
            {
                // Append mode
                _vertexBuffer.SetData(VerticesEndIndex * _declaration.VertexStride, //< where to start in the VERTEX BUFFER
                    vertices, //< what to copy
                    0, //< start index in the ARRAY
                    vertexLength, //< length of the ARRAY
                    _declaration.VertexStride, //< size of ONE vertex
                    SetDataOptions.NoOverwrite);

                _internalVerticesStartIndex += _numVertices;
                _numVertices = vertices.Length;
            }

            _numPrimitives = numPrimitives;
        }
    }
}