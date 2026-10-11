namespace LogiqueJeu.Joueur
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;

    // Les fonctions de requête dans cette classes génèrent les exceptions suivantes :
    //
    // Si la requête échoue, une exception de type `HttpRequestException` est générée.
    // -> Si la raison est le réseau (requête n'arrive même pas à sa destination et donc pas de réponse reçu)
    //    HttpRequestException.GetStatusCode() retourne null.
    // -> Si la requête est bien envoyée mais la réponse est une erreur HTTP (autre que 200)
    //    HttpRequestException.GetStatusCode() retourne le code renvoyé par l'API.
    //
    // Si les opérations asynchrones sont annulées, une exception de type `OperationCanceledException` est générée.
    public class API
    {
        public const double REQUEST_TIMEOUT = 5;

        private static readonly HttpClient SharedClient =
            new(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (Message, Cert, Chain, Errors) =>
                        true
                }
            )
            {
                BaseAddress = new Uri(Constants.API_URL),
                Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT)
            };

        public static async Task<SelfJoueurAPICompteDTO> FetchSelfJoueurAsync(
            string NomUtilisateur,
            string TokenConnexion,
            CancellationToken Token = default
        )
        {
            var RequestURL = "compte/watch/" + NomUtilisateur;
            var Request = new HttpRequestMessage(HttpMethod.Get, RequestURL);
            Request.Headers.Add("Authorization", "Bearer " + TokenConnexion);

            HttpResponseMessage httpResponseMessage = null;
            try
            {
                httpResponseMessage = await SharedClient.SendAsync(Request, Token);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
                when (httpResponseMessage != null
                    && ex.SetStatusCode(httpResponseMessage.StatusCode)
                )
            {
                // Intentionally left empty. Will never be reached.
                // See https://stackoverflow.com/a/74321346
            }

            return await httpResponseMessage.Content.ReadFromJsonAsync<SelfJoueurAPICompteDTO>(
                Token
            );
        }

        public static async Task<JoueurAPICompteDTO> FetchJoueurAsync(
            string NomUtilisateur,
            CancellationToken Token = default
        )
        {
            var RequestURL = "compte/watch/" + NomUtilisateur;

            HttpResponseMessage httpResponseMessage = null;
            try
            {
                httpResponseMessage = await SharedClient.GetAsync(RequestURL, Token);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
                when (httpResponseMessage != null
                    && ex.SetStatusCode(httpResponseMessage.StatusCode)
                )
            {
                // Intentionally left empty. Will never be reached.
                // See https://stackoverflow.com/a/74321346
            }

            return await httpResponseMessage.Content.ReadFromJsonAsync<JoueurAPICompteDTO>(Token);
        }

        public static async Task<List<Joueur>> FetchClassementsJoueurAsync(
            string NomUtilisateur,
            CancellationToken Token = default
        )
        {
            var RequestURL = "classement/" + NomUtilisateur;
            return await FetchClassementsAsync(RequestURL, Token);
        }

        public static async Task<List<Joueur>> FetchClassementsPageAsync(
            int PageNumber,
            CancellationToken Token = default
        )
        {
            var RequestURL = "classement/";
            if (PageNumber > 0)
            {
                RequestURL += PageNumber;
            }
            else if (PageNumber < 0)
            {
                throw new ArgumentException("PageNumber must be zero or positive");
            }
            return await FetchClassementsAsync(RequestURL, Token);
        }

        public static async Task<List<Joueur>> FetchClassementsTopAsync(
            int Limit,
            CancellationToken Token = default
        )
        {
            var RequestURL = "classement/top/";
            if (Limit > 0)
            {
                RequestURL += Limit;
            }
            else
            {
                throw new ArgumentException("Limit must be a positive integer");
            }
            return await FetchClassementsAsync(RequestURL, Token);
        }

        // This method fetches the order of the players in the leaderboard
        // but it does it in two stages. It first fetches the leaderboard
        // and secondly for each entry in the leaderboard, it fetches more
        // information on that specific account from a seperate endpoint.
        // Due to the nature of this two stage behaviour which is dictated by
        // the backend API implementation (there is nothing we can do about it
        // from the client side to make it better), there is a possiblity for a
        // weird pseudo error case where the leaderboard or the player details
        // of some players in the previously fetched leaderboard might have changed
        // in the meantime, making the leaderboard inconsistent.
        //
        // A good way to handle this would be to merge the two endpoints on the backend
        // or implement a database lock mechanism to prevent changes while this method executes.
        // This would then result in a consistent result no matter what with zero race conditions.
        private static async Task<List<Joueur>> FetchClassementsAsync(
            string RequestURL,
            CancellationToken Token = default
        )
        {
            HttpResponseMessage httpResponseMessage = null;
            try
            {
                httpResponseMessage = await SharedClient.GetAsync(RequestURL, Token);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
                when (httpResponseMessage != null
                    && ex.SetStatusCode(httpResponseMessage.StatusCode)
                )
            {
                // Intentionally left empty. Will never be reached.
                // See https://stackoverflow.com/a/74321346
            }

            var Response = await httpResponseMessage.Content.ReadFromJsonAsync<
                List<JoueurAPIClassementDTO>
            >(Token);

            List<Joueur> Result = new();

            var SoiMeme = JoueurManager.Instance.GetSelfJoueur();

            foreach (var JoueurDTO in Response)
            {
                Token.ThrowIfCancellationRequested();

                Joueur J;
                if (JoueurDTO.username == SoiMeme.NomUtilisateur)
                {
                    await JoueurManager.Instance.UpdateSelfJoueur();
                    SoiMeme = JoueurManager.Instance.GetSelfJoueur();
                    J = (SelfJoueur)SoiMeme.Clone();
                }
                else
                {
                    J = new GenericJoueur { NomUtilisateur = JoueurDTO.username };
                    await J.LoadSelf(Token);
                }
                J.Classement = JoueurDTO.classement;
                Result.Add(J);
            }

            return Result;
        }

        // Error handling is left to the caller. Need to handle the following cases
        // and any other ones that I may have missed mentioning:
        //
        // - User not found (Invalid username)
        // - Password mismatch (Invalid password)
        //
        // See these references for more information:
        // https://discord.com/channels/1201575577132486766/1204866892993536021/1220835503671349269
        // https://discord.com/channels/1201575577132486766/1204874375480873011/1227992255512449058
        public static async Task<SelfJoueurAPIConnexionResponseDTO> PostSelfJoueurConnexionAsync(
            SelfJoueurAPIConnexionDTO ConnexionDTO,
            CancellationToken Token = default
        )
        {
            var RequestURL = "compte/login";
            var Request = new HttpRequestMessage(HttpMethod.Post, RequestURL)
            {
                Content = JsonContent.Create(ConnexionDTO)
            };

            HttpResponseMessage httpResponseMessage = null;
            try
            {
                httpResponseMessage = await SharedClient.SendAsync(Request, Token);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
                when (httpResponseMessage != null
                    && ex.SetStatusCode(httpResponseMessage.StatusCode)
                )
            {
                // Intentionally left empty. Will never be reached.
                // See https://stackoverflow.com/a/74321346
            }

            return await httpResponseMessage.Content.ReadFromJsonAsync<SelfJoueurAPIConnexionResponseDTO>(
                Token
            );
        }

        // Error handling is left to the caller. Need to handle the following cases
        // and any other ones that I may have missed mentioning:
        //
        // - Invalid username (illegal character, already taken...)
        // - Invalid password (length, complexity...)
        //
        // See these references for more information:
        // https://discord.com/channels/1201575577132486766/1204866892993536021/1220835503671349269
        // https://discord.com/channels/1201575577132486766/1204874375480873011/1227992255512449058
        public static async Task<SelfJoueurAPIConnexionResponseDTO> PostSelfJoueurCreationAsync(
            SelfJoueurAPICreationDTO CreationDTO,
            CancellationToken Token = default
        )
        {
            var RequestURL = "compte/register";
            var Request = new HttpRequestMessage(HttpMethod.Post, RequestURL)
            {
                Content = JsonContent.Create(CreationDTO)
            };

            HttpResponseMessage httpResponseMessage = null;
            try
            {
                httpResponseMessage = await SharedClient.SendAsync(Request, Token);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
                when (httpResponseMessage != null
                    && ex.SetStatusCode(httpResponseMessage.StatusCode)
                )
            {
                // Intentionally left empty. Will never be reached.
                // See https://stackoverflow.com/a/74321346
            }

            return await httpResponseMessage.Content.ReadFromJsonAsync<SelfJoueurAPIConnexionResponseDTO>(
                Token
            );
        }
    }

    // See https://stackoverflow.com/a/74321346
    public static class HttpRequestExceptionExtensions
    {
        private const string StatusCodeKeyName = "StatusCode";

        public static bool SetStatusCode(
            this HttpRequestException httpRequestException,
            HttpStatusCode httpStatusCode
        )
        {
            httpRequestException.Data[StatusCodeKeyName] = httpStatusCode;

            return false;
        }

        public static HttpStatusCode? GetStatusCode(this HttpRequestException httpRequestException)
        {
            return (HttpStatusCode?)httpRequestException.Data[StatusCodeKeyName];
        }
    }
}
