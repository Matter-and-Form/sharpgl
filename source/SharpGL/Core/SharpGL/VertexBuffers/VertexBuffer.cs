using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGL.VertexBuffers
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Very useful reference for management of VBOs and VBAs:
    /// http://stackoverflow.com/questions/8704801/glvertexattribpointer-clarification
    /// </remarks>
    public class VertexBuffer
    {
        public void SetAttributeData(OpenGL gl, uint attributeIndex, int size, bool isNormalised)
        {
            SetAttributeData(gl, attributeIndex, size, isNormalised, 0, 0, false);
        }

        public void SetAttributeData(OpenGL gl, uint attributeIndex, int size, bool isNormalised, uint stride, bool customStrideCalculation = false)
        {
            SetAttributeData(gl, attributeIndex, size, isNormalised, stride, 0, customStrideCalculation);
        }

        public void SetAttributeData(OpenGL gl, uint attributeIndex, int size, bool isNormalised, uint stride, int offset, bool customStrideAndOffsetCalculation = false)
        {
            if (customStrideAndOffsetCalculation)
            {
                gl.VertexAttribPointer(attributeIndex, size, OpenGL.GL_FLOAT, isNormalised, stride, new IntPtr(offset));
            }
            else
            {
                gl.VertexAttribPointer(attributeIndex, size, OpenGL.GL_FLOAT, isNormalised, stride * sizeof(float), new IntPtr(offset * sizeof(float)));
            }

            gl.EnableVertexAttribArray(attributeIndex);
        }

        public void Create(OpenGL gl)
        {
            //  Generate the vertex array.
            uint[] ids = new uint[1];
            gl.GenBuffers(1, ids);
            vertexBufferObject = ids[0];
        }


        public void SetData(OpenGL gl, float[] rawData)
        {
            //  Set the data, specify its shape and assign it to a vertex attribute (so shaders can bind to it).
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, rawData, OpenGL.GL_STATIC_DRAW);
        }

        public void Bind(OpenGL gl)
        {
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vertexBufferObject);
        }

        public void Unbind(OpenGL gl)
        {
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
        }

        public bool IsCreated() { return vertexBufferObject != 0; }

        /// <summary>
        /// Gets the vertex buffer object.
        /// </summary>
        public uint VertexBufferObject
        {
            get { return vertexBufferObject; }
        }

        private uint vertexBufferObject;
    }
}
