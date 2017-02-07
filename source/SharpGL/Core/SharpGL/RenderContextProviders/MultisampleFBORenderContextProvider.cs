using SharpGL.Version;
using System;

namespace SharpGL.RenderContextProviders
{
    public class MultisampleFBORenderContextProvider : FBORenderContextProvider
    {
        private static readonly int MSAA = 4;
        /// <summary>
        /// Initializes a new instance of the <see cref="MultisampleFBORenderContextProvider"/> class.
        /// </summary>
        public MultisampleFBORenderContextProvider()
            : base()
        { }

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
            //  Call the base class.             
            base.Create(openGLVersion, gl, width, height, bitDepth, parameter);

            uint[] ids = new uint[1];
            // Multi sampled fbo
            gl.GenFramebuffersEXT(1, ids);
            msFrameBufferID = ids[0];
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, msFrameBufferID);

            // Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffersEXT(1, ids);
            msColourRenderBufferID = ids[0];
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, msColourRenderBufferID);
            gl.RenderbufferStorageMultisampleEXT(OpenGL.GL_RENDERBUFFER_EXT, MSAA, OpenGL.GL_RGBA, width, height);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_RENDERBUFFER_EXT, msColourRenderBufferID);
            ValidateFramebufferStatus(gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT));
            return true;
        }

        protected override void DestroyFramebuffers()
        {
            base.DestroyFramebuffers();
            //  Delete the render buffers.
            gl.DeleteRenderbuffersEXT(1, new uint[] { msColourRenderBufferID });

            //    Delete the framebuffer.
            gl.DeleteFramebuffersEXT(1, new uint[] { msFrameBufferID });

            //  Reset the IDs.
            msColourRenderBufferID = 0;
            msFrameBufferID = 0;
        }

        public override void SetDimensions(int width, int height)
        {
            //  Call the base.
            base.SetDimensions(width, height);

            uint[] ids = new uint[1];
            // Multi sampled fbo
            gl.GenFramebuffersEXT(1, ids);
            msFrameBufferID = ids[0];
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, msFrameBufferID);
            // Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffersEXT(1, ids);
            msColourRenderBufferID = ids[0];
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, msColourRenderBufferID);
            gl.RenderbufferStorageMultisampleEXT(OpenGL.GL_RENDERBUFFER_EXT, MSAA, OpenGL.GL_RGBA, width, height);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_RENDERBUFFER_EXT, msColourRenderBufferID);
            ValidateFramebufferStatus(gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT));
        }

        public override void Blit(IntPtr hdc)
        {
            if (deviceContextHandle == IntPtr.Zero)
                return;
            // Bind buffers to blit
            gl.BindFramebufferEXT(OpenGL.GL_READ_FRAMEBUFFER_EXT, msFrameBufferID);
            gl.BindFramebufferEXT(OpenGL.GL_DRAW_FRAMEBUFFER_EXT, frameBufferID);
            // Blit multi-sample fbo to regular fbo
            gl.BlitFramebufferEXT(0, 0, Width, Height, 0, 0, Width, Height, OpenGL.GL_COLOR_BUFFER_BIT, OpenGL.GL_LINEAR);
            // Bind single sample buffer
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, frameBufferID);
            // Use single sample buffer to blit to image.
            base.Blit(hdc);
            // Bind the multi-sample fbo for all the writes again
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, msFrameBufferID);
        }

        protected uint msColourRenderBufferID = 0;
        protected uint msFrameBufferID = 0;
    }
}
