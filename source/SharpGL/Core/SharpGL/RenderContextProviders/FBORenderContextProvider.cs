using SharpGL.Version;
using System;

namespace SharpGL.RenderContextProviders
{
    public class FBORenderContextProvider : HiddenWindowRenderContextProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FBORenderContextProvider"/> class.
        /// </summary>
        public FBORenderContextProvider()
        {
            //  We can layer GDI drawing on top of open gl drawing.
            GDIDrawingEnabled = true;
        }

        /// <summary>
        /// Creates the render context provider. Must also create the OpenGL extensions.
        /// </summary>
        /// <param name="openGLVersion">The desired OpenGL version.</param>
        /// <param name="gl">The OpenGL context.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bitDepth">The bit depth.</param>
        /// <param name="parameter">The parameter</param>
        /// <returns></returns>
        public override bool Create(OpenGLVersion openGLVersion, OpenGL gl, int width, int height, int bitDepth, object parameter)
        {
            this.gl = gl;

            //  Call the base class.             
            base.Create(openGLVersion, gl, width, height, bitDepth, parameter);

            uint[] ids = new uint[1];

            //  First, create the frame buffer and bind it.
            gl.GenFramebuffersEXT(1, ids);
            frameBufferID = ids[0];
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, frameBufferID);

            //    Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffersEXT(1, ids);
            colourRenderBufferID = ids[0];
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, colourRenderBufferID);
            gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_RGBA, width, height);

            //    Create the depth stencil render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffersEXT(1, ids);
            depthStencilRenderBufferID = ids[0];
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, depthStencilRenderBufferID);
            gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_DEPTH24_STENCIL8, width, height);

            //  Set the render buffer for colour, depth and stencil.
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT,
                OpenGL.GL_RENDERBUFFER_EXT, colourRenderBufferID);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_DEPTH_ATTACHMENT_EXT,
                OpenGL.GL_RENDERBUFFER_EXT, depthStencilRenderBufferID);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_STENCIL_ATTACHMENT_EXT,
                OpenGL.GL_RENDERBUFFER_EXT, depthStencilRenderBufferID);
            ValidateFramebufferStatus(gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT));

            dibSectionDeviceContext = Win32.CreateCompatibleDC(deviceContextHandle);

            //  Create the DIB section.
            dibSection.Create(dibSectionDeviceContext, width, height, bitDepth);
            return true;
        }

        protected virtual void DestroyFramebuffers()
        {
            //  Delete the render buffers.
            gl.DeleteRenderbuffersEXT(2, new uint[] { colourRenderBufferID, depthStencilRenderBufferID });

            //    Delete the framebuffer.
            gl.DeleteFramebuffersEXT(1, new uint[] { frameBufferID });

            //  Reset the IDs.
            colourRenderBufferID = 0;
            depthStencilRenderBufferID = 0;
            frameBufferID = 0;
        }

        public override void Destroy()
        {
            //  Delete the render buffers.
            DestroyFramebuffers();

            //  Destroy the internal dc.
            Win32.DeleteDC(dibSectionDeviceContext);

            //    Call the base, which will delete the render context handle and window.
            base.Destroy();
        }

        public override void SetDimensions(int width, int height)
        {
            //  Call the base.
            base.SetDimensions(width, height);

            //    Resize dib section.
            dibSection.Resize(width, height, BitDepth);

            DestroyFramebuffers();

            uint[] ids = new uint[1];

            //  First, create the frame buffer and bind it.
            ids = new uint[1];
            gl.GenFramebuffersEXT(1, ids);
            frameBufferID = ids[0];
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, frameBufferID);

            //    Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffersEXT(1, ids);
            colourRenderBufferID = ids[0];
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, colourRenderBufferID);
            gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_RGBA, width, height);

            //    Create the depth stencil render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffersEXT(1, ids);
            depthStencilRenderBufferID = ids[0];
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, depthStencilRenderBufferID);
            gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_DEPTH24_STENCIL8, width, height);

            //  Set the render buffer for colour, depth and stencil.
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT,
                OpenGL.GL_RENDERBUFFER_EXT, colourRenderBufferID);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_DEPTH_ATTACHMENT_EXT,
                OpenGL.GL_RENDERBUFFER_EXT, depthStencilRenderBufferID);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_STENCIL_ATTACHMENT_EXT,
                OpenGL.GL_RENDERBUFFER_EXT, depthStencilRenderBufferID);
            ValidateFramebufferStatus(gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT));
        }

        protected void ValidateFramebufferStatus(uint status)
        {
            if (status == OpenGL.GL_FRAMEBUFFER_COMPLETE_EXT)
                return;
            throw new FramebufferIncompleteException(status);
        }

        public override void Blit(IntPtr hdc)
        {
            if (deviceContextHandle == IntPtr.Zero)
                return;
            //  Set the read buffer.
            gl.ReadBuffer(OpenGL.GL_COLOR_ATTACHMENT0_EXT);

            //    Read the pixels into the DIB section.
            gl.ReadPixels(0, 0, Width, Height, OpenGL.GL_BGRA,
                OpenGL.GL_UNSIGNED_BYTE, dibSection.Bits);

            //    Blit the DC (containing the DIB section) to the target DC.
            Win32.BitBlt(hdc, 0, 0, Width, Height,
                dibSectionDeviceContext, 0, 0, Win32.SRCCOPY);
        }

        protected uint colourRenderBufferID = 0;
        protected uint depthStencilRenderBufferID = 0;
        protected uint frameBufferID = 0;
        protected IntPtr dibSectionDeviceContext = IntPtr.Zero;
        protected DIBSection dibSection = new DIBSection();
        protected OpenGL gl;

        /// <summary>
        /// Gets the internal DIB section.
        /// </summary>
        /// <value>The internal DIB section.</value>
        public DIBSection InternalDIBSection
        {
            get { return dibSection; }
        }

        private class FramebufferIncompleteException : Exception
        {
            public FramebufferIncompleteException(uint status)
                : base(GetStringFromStatus(status))
            { }

            private static string GetStringFromStatus(uint status)
            {
                switch (status)
                {
                    case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT_EXT:
                        return "36054: Incomplete Attachment";
                    case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT_EXT:
                        return "36055: Missing Attachment";
                    case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_DIMENSIONS_EXT:
                        return "36056: Incomplete Dimensions";
                    case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_FORMATS_EXT:
                        return "36057: Incomplete Formats";
                    case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER_EXT:
                        return "36058: Incomplete draw buffer";
                    case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_READ_BUFFER_EXT:
                        return "36059: Incomplete read buffer";
                    case OpenGL.GL_FRAMEBUFFER_UNSUPPORTED_EXT:
                        return "36060: Framebuffer unsupported";
                    case OpenGL.GL_INVALID_ENUM:
                        return "1280: Target is not a framebuffer";
                    default:
                        return status + ": An error has occured";
                }
            }
        }
    }
}
