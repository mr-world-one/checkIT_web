namespace CheckIT.Web.Services;

public interface IPromScraperFactory
{
    IPromScraper Create(bool headless = true);
}
