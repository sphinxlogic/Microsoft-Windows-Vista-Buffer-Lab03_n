using System; 
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization; 

namespace System.Net.Mime 
{ 
    /// <summary>
    /// Summary description for MimeMultiPart. 
    /// </summary>
    internal class MimeMultiPart:MimeBasePart
    {
        Collection<MimeBasePart> parts; 
        static int boundary;
        AsyncCallback mimePartSentCallback; 
 

        internal MimeMultiPart(MimeMultiPartType type) 
        {
            MimeMultiPartType = type;
        }
 
        internal MimeMultiPartType MimeMultiPartType {
            /* 
            // Consider removing. 
            get {
                return GetPartType(); 
            }
            */
            set {
                if (value > MimeMultiPartType.Related || value < MimeMultiPartType.Mixed) 
                {
                    throw new NotSupportedException(value.ToString()); 
                } 
                SetType(value);
            } 
        }

        void SetType(MimeMultiPartType type) {
            ContentType.MediaType = "multipart" + "/" + type.ToString().ToLower(CultureInfo.InvariantCulture); 
            ContentType.Boundary = GetNextBoundary();
        } 
 
        /*
        // Consider removing. 
        MimeMultiPartType GetPartType()
        {
            switch (ContentType.MediaType)
            { 
                case "multipart/alternative":
                    return MimeMultiPartType.Alternative; 
 
                case "multipart/mixed":
                    return MimeMultiPartType.Mixed; 

                case "multipart/parallel":
                    return MimeMultiPartType.Parallel;
 
                case "multipart/related":
                    return MimeMultiPartType.Related; 
            } 
            return MimeMultiPartType.Unknown;
        } 
        */

        internal Collection<MimeBasePart> Parts {
            get{ 
                if (parts == null) {
                    parts = new Collection<MimeBasePart>(); 
                } 
                return parts;
            } 
        }


        internal void Complete(IAsyncResult result, Exception e){ 
            //if we already completed and we got called again,
            //it mean's that there was an exception in the callback and we 
            //should just rethrow it. 

            MimePartContext context = (MimePartContext)result.AsyncState; 

            if (context.completed) {
                throw e;
            } 

            try{ 
                context.outputStream.Close(); 
            }
            catch(Exception ex){ 
                if (e == null) {
                    e = ex;
                }
            } 
            catch {
                if (e == null) { 
                    e = new Exception(SR.GetString(SR.net_nonClsCompliantException)); 
                }
            } 
            context.completed = true;
            context.result.InvokeCallback(e);
        }
 
        internal void MimeWriterCloseCallback(IAsyncResult result)
        { 
            if (result.CompletedSynchronously ) { 
               return;
            } 

            ((MimePartContext)result.AsyncState).completedSynchronously = false;

            try{ 
                MimeWriterCloseCallbackHandler(result);
            } 
            catch (Exception e) { 
                Complete(result,e);
            } 
            catch {
                Complete(result, new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            }
        } 

 
 
        void MimeWriterCloseCallbackHandler(IAsyncResult result) {
            MimePartContext context = (MimePartContext)result.AsyncState; 
            ((MimeWriter)context.writer).EndClose(result);
            Complete(result,null);
        }
 

 
        internal void MimePartSentCallback(IAsyncResult result) 
        {
            if (result.CompletedSynchronously ) { 
               return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false; 

            try{ 
                MimePartSentCallbackHandler(result); 
            }
            catch (Exception e) { 
                Complete(result,e);
            }
            catch {
                Complete(result, new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
            }
        } 
 

        void MimePartSentCallbackHandler(IAsyncResult result) 
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            MimeBasePart part = (MimeBasePart)context.partsEnumerator.Current;
            part.EndSend(result); 

            if (context.partsEnumerator.MoveNext()) { 
                part = (MimeBasePart)context.partsEnumerator.Current; 
                IAsyncResult sendResult = part.BeginSend(context.writer, mimePartSentCallback, context);
                if (sendResult.CompletedSynchronously) { 
                   MimePartSentCallbackHandler(sendResult);
                }
                return;
            } 
            else {
                IAsyncResult closeResult = ((MimeWriter)context.writer).BeginClose(new AsyncCallback(MimeWriterCloseCallback), context); 
                if (closeResult.CompletedSynchronously) { 
                   MimeWriterCloseCallbackHandler(closeResult);
                } 

            }
        }
 

 
        internal void ContentStreamCallback(IAsyncResult result) 
        {
            if (result.CompletedSynchronously ) { 
                return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false; 

            try{ 
                ContentStreamCallbackHandler(result); 
            }
            catch (Exception e) { 
                Complete(result,e);
            }
            catch {
                Complete(result, new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
            }
        } 
 

 
        void ContentStreamCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            context.outputStream = context.writer.EndGetContentStream(result); 
            context.writer = new MimeWriter(context.outputStream, ContentType.Boundary);
            if (context.partsEnumerator.MoveNext()) { 
                MimeBasePart part = (MimeBasePart)context.partsEnumerator.Current; 

                mimePartSentCallback = new AsyncCallback(MimePartSentCallback); 
                IAsyncResult sendResult = part.BeginSend(context.writer, mimePartSentCallback, context);
                if (sendResult.CompletedSynchronously) {
                   MimePartSentCallbackHandler(sendResult);
                } 
                return;
            } 
            else { 
                IAsyncResult closeResult = ((MimeWriter)context.writer).BeginClose(new AsyncCallback(MimeWriterCloseCallback),context);
                if (closeResult.CompletedSynchronously) { 
                   MimeWriterCloseCallbackHandler(closeResult);
                }
            }
        } 

 
        internal override IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, object state) 
        {
            writer.WriteHeaders(Headers); 
            MimePartAsyncResult result = new MimePartAsyncResult(this, state, callback);
            MimePartContext context = new MimePartContext(writer, result, Parts.GetEnumerator());
            IAsyncResult contentResult = writer.BeginGetContentStream(new AsyncCallback(ContentStreamCallback), context);
            if (contentResult.CompletedSynchronously) { 
               ContentStreamCallbackHandler(contentResult);
            } 
            return result; 
        }
 

        internal class MimePartContext {
            internal MimePartContext(BaseWriter writer, LazyAsyncResult result, IEnumerator<MimeBasePart> partsEnumerator) {
                this.writer = writer; 
                this.result = result;
                this.partsEnumerator = partsEnumerator; 
            } 

            internal IEnumerator<MimeBasePart> partsEnumerator; 
            internal Stream outputStream;
            internal LazyAsyncResult result;
            internal BaseWriter writer;
            internal bool completed; 
            internal bool completedSynchronously = true;
        } 
 

        internal override void Send(BaseWriter writer) { 
            writer.WriteHeaders(Headers);
            Stream outputStream = writer.GetContentStream();
            MimeWriter mimeWriter = new MimeWriter(outputStream, ContentType.Boundary);
 
            foreach (MimeBasePart part in Parts) {
                part.Send(mimeWriter); 
            } 

            mimeWriter.Close(); 
            outputStream.Close();
        }

 
        internal string GetNextBoundary() {
            string boundaryString = "--boundary_" + boundary.ToString(CultureInfo.InvariantCulture)+"_"+Guid.NewGuid().ToString(null, CultureInfo.InvariantCulture); 
 
            boundary++;
            return boundaryString; 
        }
    }
}
using System; 
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization; 

namespace System.Net.Mime 
{ 
    /// <summary>
    /// Summary description for MimeMultiPart. 
    /// </summary>
    internal class MimeMultiPart:MimeBasePart
    {
        Collection<MimeBasePart> parts; 
        static int boundary;
        AsyncCallback mimePartSentCallback; 
 

        internal MimeMultiPart(MimeMultiPartType type) 
        {
            MimeMultiPartType = type;
        }
 
        internal MimeMultiPartType MimeMultiPartType {
            /* 
            // Consider removing. 
            get {
                return GetPartType(); 
            }
            */
            set {
                if (value > MimeMultiPartType.Related || value < MimeMultiPartType.Mixed) 
                {
                    throw new NotSupportedException(value.ToString()); 
                } 
                SetType(value);
            } 
        }

        void SetType(MimeMultiPartType type) {
            ContentType.MediaType = "multipart" + "/" + type.ToString().ToLower(CultureInfo.InvariantCulture); 
            ContentType.Boundary = GetNextBoundary();
        } 
 
        /*
        // Consider removing. 
        MimeMultiPartType GetPartType()
        {
            switch (ContentType.MediaType)
            { 
                case "multipart/alternative":
                    return MimeMultiPartType.Alternative; 
 
                case "multipart/mixed":
                    return MimeMultiPartType.Mixed; 

                case "multipart/parallel":
                    return MimeMultiPartType.Parallel;
 
                case "multipart/related":
                    return MimeMultiPartType.Related; 
            } 
            return MimeMultiPartType.Unknown;
        } 
        */

        internal Collection<MimeBasePart> Parts {
            get{ 
                if (parts == null) {
                    parts = new Collection<MimeBasePart>(); 
                } 
                return parts;
            } 
        }


        internal void Complete(IAsyncResult result, Exception e){ 
            //if we already completed and we got called again,
            //it mean's that there was an exception in the callback and we 
            //should just rethrow it. 

            MimePartContext context = (MimePartContext)result.AsyncState; 

            if (context.completed) {
                throw e;
            } 

            try{ 
                context.outputStream.Close(); 
            }
            catch(Exception ex){ 
                if (e == null) {
                    e = ex;
                }
            } 
            catch {
                if (e == null) { 
                    e = new Exception(SR.GetString(SR.net_nonClsCompliantException)); 
                }
            } 
            context.completed = true;
            context.result.InvokeCallback(e);
        }
 
        internal void MimeWriterCloseCallback(IAsyncResult result)
        { 
            if (result.CompletedSynchronously ) { 
               return;
            } 

            ((MimePartContext)result.AsyncState).completedSynchronously = false;

            try{ 
                MimeWriterCloseCallbackHandler(result);
            } 
            catch (Exception e) { 
                Complete(result,e);
            } 
            catch {
                Complete(result, new Exception(SR.GetString(SR.net_nonClsCompliantException)));
            }
        } 

 
 
        void MimeWriterCloseCallbackHandler(IAsyncResult result) {
            MimePartContext context = (MimePartContext)result.AsyncState; 
            ((MimeWriter)context.writer).EndClose(result);
            Complete(result,null);
        }
 

 
        internal void MimePartSentCallback(IAsyncResult result) 
        {
            if (result.CompletedSynchronously ) { 
               return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false; 

            try{ 
                MimePartSentCallbackHandler(result); 
            }
            catch (Exception e) { 
                Complete(result,e);
            }
            catch {
                Complete(result, new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
            }
        } 
 

        void MimePartSentCallbackHandler(IAsyncResult result) 
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            MimeBasePart part = (MimeBasePart)context.partsEnumerator.Current;
            part.EndSend(result); 

            if (context.partsEnumerator.MoveNext()) { 
                part = (MimeBasePart)context.partsEnumerator.Current; 
                IAsyncResult sendResult = part.BeginSend(context.writer, mimePartSentCallback, context);
                if (sendResult.CompletedSynchronously) { 
                   MimePartSentCallbackHandler(sendResult);
                }
                return;
            } 
            else {
                IAsyncResult closeResult = ((MimeWriter)context.writer).BeginClose(new AsyncCallback(MimeWriterCloseCallback), context); 
                if (closeResult.CompletedSynchronously) { 
                   MimeWriterCloseCallbackHandler(closeResult);
                } 

            }
        }
 

 
        internal void ContentStreamCallback(IAsyncResult result) 
        {
            if (result.CompletedSynchronously ) { 
                return;
            }

            ((MimePartContext)result.AsyncState).completedSynchronously = false; 

            try{ 
                ContentStreamCallbackHandler(result); 
            }
            catch (Exception e) { 
                Complete(result,e);
            }
            catch {
                Complete(result, new Exception(SR.GetString(SR.net_nonClsCompliantException))); 
            }
        } 
 

 
        void ContentStreamCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState;
            context.outputStream = context.writer.EndGetContentStream(result); 
            context.writer = new MimeWriter(context.outputStream, ContentType.Boundary);
            if (context.partsEnumerator.MoveNext()) { 
                MimeBasePart part = (MimeBasePart)context.partsEnumerator.Current; 

                mimePartSentCallback = new AsyncCallback(MimePartSentCallback); 
                IAsyncResult sendResult = part.BeginSend(context.writer, mimePartSentCallback, context);
                if (sendResult.CompletedSynchronously) {
                   MimePartSentCallbackHandler(sendResult);
                } 
                return;
            } 
            else { 
                IAsyncResult closeResult = ((MimeWriter)context.writer).BeginClose(new AsyncCallback(MimeWriterCloseCallback),context);
                if (closeResult.CompletedSynchronously) { 
                   MimeWriterCloseCallbackHandler(closeResult);
                }
            }
        } 

 
        internal override IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, object state) 
        {
            writer.WriteHeaders(Headers); 
            MimePartAsyncResult result = new MimePartAsyncResult(this, state, callback);
            MimePartContext context = new MimePartContext(writer, result, Parts.GetEnumerator());
            IAsyncResult contentResult = writer.BeginGetContentStream(new AsyncCallback(ContentStreamCallback), context);
            if (contentResult.CompletedSynchronously) { 
               ContentStreamCallbackHandler(contentResult);
            } 
            return result; 
        }
 

        internal class MimePartContext {
            internal MimePartContext(BaseWriter writer, LazyAsyncResult result, IEnumerator<MimeBasePart> partsEnumerator) {
                this.writer = writer; 
                this.result = result;
                this.partsEnumerator = partsEnumerator; 
            } 

            internal IEnumerator<MimeBasePart> partsEnumerator; 
            internal Stream outputStream;
            internal LazyAsyncResult result;
            internal BaseWriter writer;
            internal bool completed; 
            internal bool completedSynchronously = true;
        } 
 

        internal override void Send(BaseWriter writer) { 
            writer.WriteHeaders(Headers);
            Stream outputStream = writer.GetContentStream();
            MimeWriter mimeWriter = new MimeWriter(outputStream, ContentType.Boundary);
 
            foreach (MimeBasePart part in Parts) {
                part.Send(mimeWriter); 
            } 

            mimeWriter.Close(); 
            outputStream.Close();
        }

 
        internal string GetNextBoundary() {
            string boundaryString = "--boundary_" + boundary.ToString(CultureInfo.InvariantCulture)+"_"+Guid.NewGuid().ToString(null, CultureInfo.InvariantCulture); 
 
            boundary++;
            return boundaryString; 
        }
    }
}
