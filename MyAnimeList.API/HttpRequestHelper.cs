using System.Net;

namespace MyAnimeList.API
{
    public class HttpRequestHelper
    {
        public static void HandleHttpCodes(HttpStatusCode code)
        {
            switch ((int)code)
            {
                case (int)HttpStatusCode.OK:
                    break;

                case (int)HttpStatusCode.NoContent:
                    throw new ServiceException("Content not found") { HttpStatusCode = (int)code };

                case (int)HttpStatusCode.MultipleChoices:
                case (int)HttpStatusCode.MovedPermanently:
                case (int)HttpStatusCode.Redirect:
                case (int)HttpStatusCode.SeeOther:
                case (int)HttpStatusCode.TemporaryRedirect:
                    throw new ServiceException(string.Format("No response from service {0}", (int)code));

                case (int)HttpStatusCode.NotModified:
                case (int)HttpStatusCode.BadRequest:
                case (int)HttpStatusCode.Forbidden:
                case (int)HttpStatusCode.NotFound:
                case (int)HttpStatusCode.MethodNotAllowed:
                case (int)HttpStatusCode.NotAcceptable:
                case (int)HttpStatusCode.RequestTimeout:
                case (int)HttpStatusCode.Gone:
                case (int)HttpStatusCode.LengthRequired:
                case (int)HttpStatusCode.PreconditionFailed:
                case (int)HttpStatusCode.RequestEntityTooLarge:
                case (int)HttpStatusCode.RequestUriTooLong:
                case (int)HttpStatusCode.UnsupportedMediaType:
                case (int)HttpStatusCode.ExpectationFailed:
                case (int)HttpStatusCode.HttpVersionNotSupported:
                    throw new ServiceException("Bad request");

                case 418:
                    throw new ServiceException(string.Format("Server is busy {0}", (int)code));

                case (int)HttpStatusCode.UseProxy:
                case (int)HttpStatusCode.Unauthorized:
                case 306:
                case 450:
                    throw new ServiceException(string.Format("Server connection error {0}", (int)code)) { HttpStatusCode = (int)code };


                case (int)HttpStatusCode.InternalServerError:
                case (int)HttpStatusCode.NotImplemented:
                case (int)HttpStatusCode.BadGateway:
                case (int)HttpStatusCode.ServiceUnavailable:
                case (int)HttpStatusCode.GatewayTimeout:
                    throw new ServiceException(string.Format("Server maintenance {0}", (int)code));
            }
        }
    }
}