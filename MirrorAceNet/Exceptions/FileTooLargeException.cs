using System;

namespace MirrorAceNet.Exceptions
{
    /// <summary>
    /// Thrown if supplied file exceeds maximum upload size for a single file
    /// </summary>
    public class FileTooLargeException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public FileTooLargeException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public FileTooLargeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public FileTooLargeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}