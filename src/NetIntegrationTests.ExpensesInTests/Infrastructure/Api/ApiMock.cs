using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.Api
{
	public class ApiMock : IAsyncDisposable
	{
		private readonly string _url;
		private readonly IEndpoint[] _endpoints;
		private readonly ConcurrentBag<PostEndpointRequest> _requests = new ConcurrentBag<PostEndpointRequest>();
		private int _postRequestsIndex = 0;
		private WebApplication _app;

		public ApiMock(string url, params IEndpoint[] endpoints)
		{
			_url = url;
			_endpoints = endpoints;
		}

		public void Start()
		{
			var builder = WebApplication.CreateBuilder();
			builder.WebHost.UseUrls(_url);
			builder.Services
				.AddMvc(_ =>
				{
				})
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
				});

			_app = builder.Build();

			foreach (var endpoint in _endpoints)
			{
				if (endpoint.CustomMapping != null)
				{
					endpoint.CustomMapping(_app, AssignRequestedMethod);
				}
				else
				{
					switch (endpoint.HttpMethod)
					{
						case HttpMethod.Get:
							_app.MapGet(endpoint.Url, (HttpContext httpContext) =>
							{
								if (endpoint.ResultPerQueryParamsString != null &&
								    endpoint.ResultPerQueryParamsString.Any())
								{
									var resultForQueryParams = endpoint
										.ResultPerQueryParamsString
										.FirstOrDefault(x => x.Item1 == httpContext.Request.QueryString.ToString());
									
									if(resultForQueryParams.Item2 != null)
										return JsonConvert.SerializeObject(resultForQueryParams.Item2); 
									
									return (object)Results.NotFound(); 
								}
								
								AssignRequestedMethod(new JsonObject(), endpoint.Url);

								if (endpoint.HttpCode == HttpStatusCode.NotFound)
								{
									return (object)Results.NotFound();
								}

								return JsonConvert.SerializeObject(endpoint.Result);
							});
							break;
						case HttpMethod.Post:
							_app.MapPost(endpoint.Url, (JsonObject body) =>
							{
								if(endpoint.HttpCode != HttpStatusCode.OK)
									return (object)Results.StatusCode((int)endpoint.HttpCode);
								
								AssignRequestedMethod(body, endpoint.Url);
								return JsonConvert.SerializeObject(endpoint.Result);
							});
							break;
						case HttpMethod.Put:
							// If PUT with body is needed in the future then it has to be refactored
							_app.MapPut(endpoint.Url, () =>
							{
								AssignRequestedMethod(new JsonObject(), endpoint.Url);
								return JsonConvert.SerializeObject(endpoint.Result);
							});
							break;
					}
				}
			}

			_app.Start();
		}

		private void AssignRequestedMethod(JsonObject jsonObject, string url)
		{
			_requests.Add(new PostEndpointRequest()
			{
				Url = url,
				BodyJson = jsonObject.ToJsonString(),
				Sequence = _postRequestsIndex += 1
			});
		}

		public List<PostEndpointRequest> GetRequestedMethods() => _requests.ToList();

		public async ValueTask DisposeAsync()
		{
			await _app.DisposeAsync();
		}
	}

	public delegate void AssignRequestedMethodDelegate(JsonObject jsonObject, string url);

	public interface IEndpoint
	{
		HttpMethod HttpMethod { get; set; }
		string Url { get; set; }
		object Result { get; set; }
		HttpStatusCode HttpCode { get; set; }
		public List<(string, object)> ResultPerQueryParamsString { get; set; }
		Dictionary<string, string> ResponseHeaders { get; set; }
		Action<WebApplication, AssignRequestedMethodDelegate>? CustomMapping { get; set; }
	}

	public class Endpoint : IEndpoint
	{
		public HttpMethod HttpMethod { get; set; }
		public string Url { get; set; }
		public object Result { get; set; }
		public List<(string, object)> ResultPerQueryParamsString { get; set; }
		public HttpStatusCode HttpCode { get; set; } = HttpStatusCode.OK;
		public Dictionary<string, string> ResponseHeaders { get; set; }
		public Action<WebApplication, AssignRequestedMethodDelegate>? CustomMapping { get; set; } = null;
	}

	public class PostEndpointRequest
	{
		public string Url { get; set; }
		public string BodyJson { get; set; }
		public int Sequence { get; set; }
	}

	public enum HttpMethod
	{
		Get,
		Post,
		Put
	}
}