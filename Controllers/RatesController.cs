using ASP_201.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP_201.Controllers
{
    [Route("api/rates")]
    [ApiController]
    public class RatesController : ControllerBase
    {
        private readonly DataContext _dataContext;

        public RatesController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        public object Get([FromQuery] String data)  // прийом даних
        {
            return new { result = $"Запит оброблено методом GET і прийнято дані {data}" };
        }

        [HttpPost]
        public object Post([FromBody] BodyData bodyData)
        {
            int statusCode;
            String result;

            if(bodyData == null
                || bodyData.Data == null
                || bodyData.ItemId == null
                || bodyData.UserId == null)
            {
                statusCode = StatusCodes.Status400BadRequest;
                result = $"Не всі дані передані: Data={bodyData?.Data} ItemId={bodyData?.ItemId} UserId={bodyData?.UserId}";
            }
            else
            {
                try
                {
                    Guid itemId = Guid.Parse(bodyData.ItemId);
                    Guid userId = Guid.Parse(bodyData.UserId);
                    int  rating = Convert.ToInt32(bodyData.Data);

                    if(_dataContext.Rates.Any(r => r.UserId == userId && r.ItemId == itemId))
                    {
                        statusCode = StatusCodes.Status406NotAcceptable;
                        result = $"Дані вже наявні: ItemId={bodyData?.ItemId} UserId={bodyData?.UserId}";
                    }
                    else
                    {
                        _dataContext.Rates.Add(new()
                        {
                            ItemId = itemId,
                            UserId = userId,
                            Rating = rating
                        });
                        _dataContext.SaveChanges();
                        statusCode = StatusCodes.Status201Created;
                        result = $"Дані внесено: Data={bodyData?.Data} ItemId={bodyData?.ItemId} UserId={bodyData?.UserId}";
                    }
                }
                catch
                {
                    statusCode = StatusCodes.Status400BadRequest;
                    result = $"Дані не опрацьовані: Data={bodyData?.Data} ItemId={bodyData?.ItemId} UserId={bodyData?.UserId}";
                }
            }
            
            HttpContext.Response.StatusCode = statusCode;
            return new { result };
        }

        public object Default()
        {
            switch (HttpContext.Request.Method)
            {
                case "LINK": return Link();

                default: throw new NotImplementedException();
            }
            
        }
        private object Link()
        {
            return new
            {
                result = $"Запит оброблено методом LINK і прийнято дані -- "
            };
        }
    }
    public class BodyData
    {
        public String? Data   { get; set; }
        public String? ItemId { get; set; }
        public String? UserId { get; set; }
    }
}
