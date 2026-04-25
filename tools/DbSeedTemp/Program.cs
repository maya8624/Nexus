using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;
using PropertyTypeEntity = Nexus.Domain.Entities.PropertyType;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.Development.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")!;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options;

using var db = new AppDbContext(options);

var rng = new Random(42);
var now = DateTimeOffset.UtcNow;

// ---------------------------------------------------------------------------
// Wipe (FK-safe order)
// ---------------------------------------------------------------------------
Console.WriteLine("Wiping existing data...");
await db.Database.ExecuteSqlRawAsync(@"
    TRUNCATE TABLE
        inspection_bookings,
        enquiries,
        inspection_slots,
        listings,
        property_images,
        property_addresses,
        properties,
        agents,
        agencies,
        property_types,
        users
    CASCADE");

// ---------------------------------------------------------------------------
// Property Types
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding property types...");
var propertyTypes = new List<PropertyTypeEntity>
{
    new() { Id = 1, Name = "House",      IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
    new() { Id = 2, Name = "Apartment",  IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
    new() { Id = 3, Name = "Townhouse",  IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
    new() { Id = 4, Name = "Villa",      IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
    new() { Id = 5, Name = "Land",       IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
};
db.PropertyTypes.AddRange(propertyTypes);
await db.SaveChangesAsync();

// ---------------------------------------------------------------------------
// Agency
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding agency...");
var agencyId = Guid.NewGuid();
db.Agencies.Add(new Agency
{
    Id            = agencyId,
    Name          = "Harbour Realty Group",
    Abn           = "72 003 547 708",
    LicenseNumber = "10014934",
    Email         = "info@harbourrealty.com.au",
    PhoneNumber   = "02 9283 1033",
    WebsiteUrl    = "https://www.harbourrealty.com.au",
    IsActive      = true,
    CreatedAtUtc  = now,
    UpdatedAtUtc  = now,
});
await db.SaveChangesAsync();

// ---------------------------------------------------------------------------
// Agents
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding agents...");
var agentData = new[]
{
    ("James", "Mitchell",  "j.mitchell@harbourrealty.com.au",  "0411 234 567", "Senior Property Manager"),
    ("Sophie","Williams",  "s.williams@harbourrealty.com.au",  "0412 345 678", "Principal"),
    ("Liam",  "Chen",      "l.chen@harbourrealty.com.au",      "0413 456 789", "Sales Agent"),
    ("Emma",  "Thompson",  "e.thompson@harbourrealty.com.au",  "0414 567 890", "Property Manager"),
    ("Noah",  "Patel",     "n.patel@harbourrealty.com.au",     "0415 678 901", "Sales Agent"),
    ("Olivia","Nguyen",    "o.nguyen@harbourrealty.com.au",    "0416 789 012", "Buyer's Agent"),
    ("Ethan", "Harris",    "e.harris@harbourrealty.com.au",    "0417 890 123", "Sales Agent"),
    ("Ava",   "Johnson",   "a.johnson@harbourrealty.com.au",   "0418 901 234", "Property Manager"),
    ("Lucas", "Anderson",  "l.anderson@harbourrealty.com.au",  "0419 012 345", "Sales Agent"),
    ("Mia",   "Robinson",  "m.robinson@harbourrealty.com.au",  "0420 123 456", "Leasing Consultant"),
};

var agentIds = agentData.Select(_ => Guid.NewGuid()).ToArray();

for (int i = 0; i < agentData.Length; i++)
{
    var (first, last, email, phone, title) = agentData[i];
    db.Agents.Add(new Agent
    {
        Id            = agentIds[i],
        AgencyId      = agencyId,
        FirstName     = first,
        LastName      = last,
        Email         = email,
        PhoneNumber   = phone,
        LicenseNumber = $"RE{20000 + i}",
        PositionTitle = title,
        Bio           = $"{first} {last} is an experienced {title} specialising in Sydney metropolitan properties.",
        IsActive      = true,
        CreatedAtUtc  = now,
        UpdatedAtUtc  = now,
    });
}
await db.SaveChangesAsync();

// ---------------------------------------------------------------------------
// Reference data
// ---------------------------------------------------------------------------
var suburbs = new[]
{
    ("Sydney",           "NSW", "2000", -33.8688m, 151.2093m),
    ("Surry Hills",      "NSW", "2010", -33.8865m, 151.2094m),
    ("Newtown",          "NSW", "2042", -33.8967m, 151.1787m),
    ("Glebe",            "NSW", "2037", -33.8788m, 151.1868m),
    ("Pyrmont",          "NSW", "2009", -33.8697m, 151.1935m),
    ("Darlinghurst",     "NSW", "2010", -33.8779m, 151.2200m),
    ("Paddington",       "NSW", "2021", -33.8839m, 151.2300m),
    ("Bondi",            "NSW", "2026", -33.8915m, 151.2767m),
    ("Bondi Junction",   "NSW", "2022", -33.8916m, 151.2501m),
    ("Coogee",           "NSW", "2034", -33.9219m, 151.2576m),
    ("Maroubra",         "NSW", "2035", -33.9467m, 151.2536m),
    ("Randwick",         "NSW", "2031", -33.9151m, 151.2399m),
    ("Kensington",       "NSW", "2033", -33.9047m, 151.2227m),
    ("Redfern",          "NSW", "2016", -33.8928m, 151.2021m),
    ("Erskineville",     "NSW", "2043", -33.9020m, 151.1890m),
    ("Alexandria",       "NSW", "2015", -33.9100m, 151.1960m),
    ("Waterloo",         "NSW", "2017", -33.9001m, 151.2069m),
    ("Zetland",          "NSW", "2017", -33.9063m, 151.2097m),
    ("Rosebery",         "NSW", "2018", -33.9165m, 151.2045m),
    ("Mascot",           "NSW", "2020", -33.9250m, 151.1916m),
    ("Marrickville",     "NSW", "2204", -33.9114m, 151.1590m),
    ("Dulwich Hill",     "NSW", "2203", -33.9058m, 151.1441m),
    ("Leichhardt",       "NSW", "2040", -33.8833m, 151.1574m),
    ("Balmain",          "NSW", "2041", -33.8606m, 151.1804m),
    ("Rozelle",          "NSW", "2039", -33.8680m, 151.1710m),
    ("Annandale",        "NSW", "2038", -33.8783m, 151.1719m),
    ("Chippendale",      "NSW", "2008", -33.8875m, 151.1977m),
    ("Camperdown",       "NSW", "2050", -33.8925m, 151.1797m),
    ("Stanmore",         "NSW", "2048", -33.8977m, 151.1603m),
    ("Ashfield",         "NSW", "2131", -33.8881m, 151.1244m),
    ("Burwood",          "NSW", "2134", -33.8774m, 151.1044m),
    ("Strathfield",      "NSW", "2135", -33.8726m, 151.0834m),
    ("Rhodes",           "NSW", "2138", -33.8283m, 151.0869m),
    ("Meadowbank",       "NSW", "2114", -33.8202m, 151.0864m),
    ("Ryde",             "NSW", "2112", -33.8162m, 151.1012m),
    ("Macquarie Park",   "NSW", "2113", -33.7780m, 151.1200m),
    ("Chatswood",        "NSW", "2067", -33.7970m, 151.1816m),
    ("Lane Cove",        "NSW", "2066", -33.8173m, 151.1676m),
    ("St Leonards",      "NSW", "2065", -33.8234m, 151.1948m),
    ("Crows Nest",       "NSW", "2065", -33.8280m, 151.2041m),
    ("North Sydney",     "NSW", "2060", -33.8393m, 151.2077m),
    ("Neutral Bay",      "NSW", "2089", -33.8373m, 151.2188m),
    ("Mosman",           "NSW", "2088", -33.8282m, 151.2434m),
    ("Manly",            "NSW", "2095", -33.7969m, 151.2858m),
    ("Dee Why",          "NSW", "2099", -33.7529m, 151.2876m),
    ("Parramatta",       "NSW", "2150", -33.8148m, 151.0017m),
    ("Westmead",         "NSW", "2145", -33.8058m, 150.9873m),
    ("Granville",        "NSW", "2142", -33.8316m, 151.0139m),
    ("Auburn",           "NSW", "2144", -33.8491m, 151.0321m),
    ("Lidcombe",         "NSW", "2141", -33.8646m, 151.0447m),
    ("Blacktown",        "NSW", "2148", -33.7670m, 150.9080m),
    ("Seven Hills",      "NSW", "2147", -33.7726m, 150.9353m),
    ("Toongabbie",       "NSW", "2146", -33.7985m, 150.9708m),
    ("Wentworthville",   "NSW", "2145", -33.8104m, 150.9625m),
    ("Pendle Hill",      "NSW", "2145", -33.7944m, 150.9596m),
    ("Merrylands",       "NSW", "2160", -33.8366m, 151.0019m),
    ("Guildford",        "NSW", "2161", -33.8520m, 150.9896m),
    ("Fairfield",        "NSW", "2165", -33.8731m, 150.9571m),
    ("Cabramatta",       "NSW", "2166", -33.8947m, 150.9414m),
    ("Liverpool",        "NSW", "2170", -33.9200m, 150.9235m),
    ("Bankstown",        "NSW", "2200", -33.9175m, 151.0343m),
    ("Lakemba",          "NSW", "2195", -33.9228m, 151.0780m),
    ("Punchbowl",        "NSW", "2196", -33.9306m, 151.0559m),
    ("Wiley Park",       "NSW", "2195", -33.9245m, 151.0694m),
    ("Campsie",          "NSW", "2194", -33.9103m, 151.1040m),
    ("Canterbury",       "NSW", "2193", -33.9041m, 151.1213m),
    ("Hurlstone Park",   "NSW", "2193", -33.9063m, 151.1310m),
    ("Earlwood",         "NSW", "2206", -33.9231m, 151.1237m),
    ("Bexley",           "NSW", "2207", -33.9475m, 151.1045m),
    ("Rockdale",         "NSW", "2216", -33.9525m, 151.1362m),
    ("Arncliffe",        "NSW", "2205", -33.9380m, 151.1466m),
    ("Wolli Creek",      "NSW", "2205", -33.9299m, 151.1559m),
    ("Kogarah",          "NSW", "2217", -33.9635m, 151.1330m),
    ("Hurstville",       "NSW", "2220", -33.9674m, 151.1023m),
    ("Mortdale",         "NSW", "2223", -33.9799m, 151.0819m),
    ("Penshurst",        "NSW", "2222", -33.9694m, 151.0837m),
    ("Riverwood",        "NSW", "2210", -33.9513m, 151.0640m),
    ("Padstow",          "NSW", "2211", -33.9573m, 151.0346m),
    ("Revesby",          "NSW", "2212", -33.9495m, 151.0134m),
    ("Panania",          "NSW", "2213", -33.9490m, 150.9983m),
    ("Condell Park",     "NSW", "2200", -33.9279m, 150.9985m),
    ("Bass Hill",        "NSW", "2197", -33.9066m, 150.9896m),
    ("Yagoona",          "NSW", "2199", -33.9124m, 151.0206m),
    ("Wiley Park",       "NSW", "2195", -33.9264m, 151.0707m),
    ("Homebush Bay",     "NSW", "2127", -33.8542m, 151.0693m),
    ("Wentworth Point",  "NSW", "2127", -33.8335m, 151.0636m),
    ("Newington",        "NSW", "2127", -33.8467m, 151.0527m),
    ("Silverwater",      "NSW", "2128", -33.8329m, 151.0415m),
    ("Ermington",        "NSW", "2115", -33.8183m, 151.0480m),
    ("Rydalmere",        "NSW", "2116", -33.8162m, 151.0260m),
    ("Dundas",           "NSW", "2117", -33.8072m, 151.0157m),
    ("Telopea",          "NSW", "2117", -33.7968m, 151.0190m),
    ("Carlingford",      "NSW", "2118", -33.7793m, 151.0457m),
    ("Epping",           "NSW", "2121", -33.7725m, 151.0810m),
    ("Eastwood",         "NSW", "2122", -33.7912m, 151.0797m),
    ("West Ryde",        "NSW", "2114", -33.8081m, 151.0774m),
    ("Gladesville",      "NSW", "2111", -33.8307m, 151.1376m),
    ("Hunters Hill",     "NSW", "2110", -33.8344m, 151.1512m),
    ("Putney",           "NSW", "2112", -33.8265m, 151.1110m),
    ("Drummoyne",        "NSW", "2047", -33.8511m, 151.1547m),
};

var streetNames = new[]
{
    "George St", "King St", "Oxford St", "Crown St", "Cleveland St",
    "Enmore Rd", "Parramatta Rd", "Victoria Rd", "Pacific Hwy", "Military Rd",
    "Anzac Pde", "Bourke St", "Elizabeth St", "Campbell St", "Pitt St",
    "Castlereagh St", "Riley St", "Albion St", "Fitzroy St", "Wilson St",
    "Church St", "Station St", "Railway Pde", "High St", "Bridge Rd",
    "Park Ave", "Hill St", "River Rd", "Bay St", "Beach Rd",
};

var descriptionTemplates = new[]
{
    "Perfectly positioned in the heart of {suburb}, this {type} offers an exceptional lifestyle opportunity. Featuring contemporary finishes throughout, this property is ideal for professionals and families alike.",
    "Welcome to this stunning {type} in sought-after {suburb}. Boasting spacious living areas, modern kitchen and seamless indoor-outdoor flow, this home represents the best of Sydney living.",
    "Nestled on a quiet street in {suburb}, this charming {type} combines period character with modern comforts. High ceilings, polished floorboards and quality renovations throughout.",
    "Discover this beautifully presented {type} in vibrant {suburb}. Walking distance to cafes, restaurants, shops and public transport. An outstanding opportunity not to be missed.",
    "This north-facing {type} in desirable {suburb} captures beautiful natural light throughout. Generous proportions, stylish interiors and a prime location make this an exceptional find.",
};

// ---------------------------------------------------------------------------
// Properties
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding 1000 properties...");
const int totalProperties = 1000;
var propertyIds = Enumerable.Range(0, totalProperties).Select(_ => Guid.NewGuid()).ToArray();
var propertyTypeIds = new[] { 1, 2, 3, 4, 5 };
var propertyTypeWeights = new[] { 30, 40, 20, 8, 2 }; // % distribution

int PickPropertyTypeId()
{
    var roll = rng.Next(100);
    var cumulative = 0;
    for (int i = 0; i < propertyTypeWeights.Length; i++)
    {
        cumulative += propertyTypeWeights[i];
        if (roll < cumulative) return propertyTypeIds[i];
    }
    return 2;
}

var properties = new List<Property>();
for (int i = 0; i < totalProperties; i++)
{
    var typeId = PickPropertyTypeId();
    var agentId = agentIds[rng.Next(agentIds.Length)];
    var suburb = suburbs[rng.Next(suburbs.Length)];
    var typeName = typeId switch { 1 => "House", 2 => "Apartment", 3 => "Townhouse", 4 => "Villa", _ => "Land" };

    var bedrooms  = typeId == 5 ? 0 : typeId == 2 ? rng.Next(1, 4) : rng.Next(2, 6);
    var bathrooms = typeId == 5 ? 0 : Math.Max(1, bedrooms - rng.Next(0, 2));
    var carSpaces = typeId == 5 ? 0 : rng.Next(0, bedrooms < 3 ? 2 : 3);

    var descTemplate = descriptionTemplates[rng.Next(descriptionTemplates.Length)];
    var description  = descTemplate.Replace("{suburb}", suburb.Item1).Replace("{type}", typeName.ToLower());

    var bedroomLabel = bedrooms > 0 ? $"{bedrooms}BR " : "";
    properties.Add(new Property
    {
        Id              = propertyIds[i],
        PropertyTypeId  = typeId,
        AgencyId        = agencyId,
        AgentId         = agentId,
        Title           = $"{bedroomLabel}{typeName} in {suburb.Item1}",
        Description     = description,
        Bedrooms        = bedrooms,
        Bathrooms       = bathrooms,
        CarSpaces       = carSpaces,
        LandSizeSqm     = typeId is 1 or 5 ? rng.Next(250, 800) : null,
        BuildingSizeSqm = typeId != 5 ? rng.Next(60, 400) : null,
        YearBuilt       = rng.Next(1970, 2024),
        IsActive        = true,
        CreatedAtUtc    = now.AddDays(-rng.Next(1, 730)),
        UpdatedAtUtc    = now,
    });
}

db.Properties.AddRange(properties);
await db.SaveChangesAsync();
Console.WriteLine("  Properties saved.");

// ---------------------------------------------------------------------------
// Property Addresses
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding property addresses...");
var addresses = new List<PropertyAddress>();
for (int i = 0; i < totalProperties; i++)
{
    var suburb = suburbs[rng.Next(suburbs.Length)];
    var street = streetNames[rng.Next(streetNames.Length)];
    var streetNumber = rng.Next(1, 200);
    var (suburbName, state, postcode, lat, lng) = suburb;

    addresses.Add(new PropertyAddress
    {
        PropertyId   = propertyIds[i],
        AddressLine1 = $"{streetNumber} {street}",
        Suburb       = suburbName,
        State        = state,
        Postcode     = postcode,
        Country      = "Australia",
        Latitude     = lat + (decimal)(rng.NextDouble() * 0.01 - 0.005),
        Longitude    = lng + (decimal)(rng.NextDouble() * 0.01 - 0.005),
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    });
}

db.PropertyAddresses.AddRange(addresses);
await db.SaveChangesAsync();
Console.WriteLine("  Addresses saved.");

// ---------------------------------------------------------------------------
// Property Images (3–5 per property) — real Unsplash photos by room type
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding property images...");

// Each entry: (url, caption)
var photosByCategory = new Dictionary<string, (string Url, string Caption)[]>
{
    ["exterior"] =
    [
        ("https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=800&q=80", "Attractive street-facing facade"),
        ("https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=800&q=80", "Modern home exterior with landscaped garden"),
        ("https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=800&q=80", "Stylish contemporary facade"),
        ("https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=800&q=80", "Welcoming home entrance"),
        ("https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800&q=80", "Impressive double-storey home"),
    ],
    ["living-room"] =
    [
        ("https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=800&q=80", "Spacious living room with natural light"),
        ("https://images.unsplash.com/photo-1586023492125-27b2c045efd3?w=800&q=80", "Open-plan living with quality finishes"),
        ("https://images.unsplash.com/photo-1560185007-cde436f6cb91?w=800&q=80", "Bright and airy living area"),
        ("https://images.unsplash.com/photo-1600210492493-0946911123ea?w=800&q=80", "Contemporary lounge with high ceilings"),
        ("https://images.unsplash.com/photo-1583847268964-b28dc8f51f92?w=800&q=80", "Stylish living space with premium flooring"),
    ],
    ["kitchen"] =
    [
        ("https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&q=80", "Modern kitchen with stone benchtops"),
        ("https://images.unsplash.com/photo-1565538810087-ef39898cf6f4?w=800&q=80", "Gourmet kitchen with island bench"),
        ("https://images.unsplash.com/photo-1556909172-54557c7e4fb7?w=800&q=80", "Designer kitchen with quality appliances"),
        ("https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&q=80", "Sleek open-plan kitchen"),
        ("https://images.unsplash.com/photo-1484154218962-a197022b5858?w=800&q=80", "Bright kitchen with ample storage"),
    ],
    ["bedroom"] =
    [
        ("https://images.unsplash.com/photo-1540518614846-7eded433c457?w=800&q=80", "Generous master bedroom with built-in wardrobes"),
        ("https://images.unsplash.com/photo-1588046130717-0eb0c9a3ba15?w=800&q=80", "Peaceful bedroom retreat"),
        ("https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?w=800&q=80", "Bright bedroom with quality carpet"),
        ("https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=800&q=80", "Spacious bedroom with natural light"),
        ("https://images.unsplash.com/photo-1616594039964-ae9021a400a0?w=800&q=80", "Elegant master suite"),
    ],
    ["bathroom"] =
    [
        ("https://images.unsplash.com/photo-1552321554-5fefe8c9ef14?w=800&q=80", "Stylish bathroom with quality fixtures"),
        ("https://images.unsplash.com/photo-1620626011761-9be5cdc82e2c?w=800&q=80", "Luxurious ensuite with freestanding bath"),
        ("https://images.unsplash.com/photo-1584622650111-993a426fbf0a?w=800&q=80", "Modern bathroom with floor-to-ceiling tiles"),
        ("https://images.unsplash.com/photo-1600566752355-35792bedcfea?w=800&q=80", "Bright bathroom with frameless shower"),
    ],
    ["backyard"] =
    [
        ("https://images.unsplash.com/photo-1558904541-efa843a96f01?w=800&q=80", "Private outdoor entertaining area"),
        ("https://images.unsplash.com/photo-1416879595882-d3ca4e0b7b2e?w=800&q=80", "Lush landscaped garden"),
        ("https://images.unsplash.com/photo-1600210491892-03d54f9e2116?w=800&q=80", "Alfresco entertaining with timber deck"),
        ("https://images.unsplash.com/photo-1598902108854-10e335adac99?w=800&q=80", "Sun-drenched courtyard garden"),
    ],
    ["dining"] =
    [
        ("https://images.unsplash.com/photo-1449247709596-a31d60a17f8a?w=800&q=80", "Open-plan dining area"),
        ("https://images.unsplash.com/photo-1617806118233-18e1de247200?w=800&q=80", "Elegant dining room with designer lighting"),
        ("https://images.unsplash.com/photo-1615876234886-fd9a39fda97f?w=800&q=80", "Spacious dining with garden views"),
    ],
    ["study"] =
    [
        ("https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=800&q=80", "Dedicated home office/study"),
        ("https://images.unsplash.com/photo-1524758631624-e2822e304c36?w=800&q=80", "Bright study with built-in shelving"),
        ("https://images.unsplash.com/photo-1611269154421-4e27233ac5c7?w=800&q=80", "Quiet home office with natural light"),
    ],
};

// First image is always exterior, then random interior shots
var interiorCategories = new[] { "living-room", "kitchen", "bedroom", "bathroom", "backyard", "dining", "study" };

var images = new List<PropertyImage>();
for (int i = 0; i < totalProperties; i++)
{
    var count = rng.Next(3, 6);
    var usedCategories = new HashSet<string>();

    for (int j = 0; j < count; j++)
    {
        string category;
        if (j == 0)
        {
            category = "exterior";
        }
        else
        {
            // Pick a category not yet used for this property
            var available = interiorCategories.Where(c => !usedCategories.Contains(c)).ToArray();
            category = available.Length > 0
                ? available[rng.Next(available.Length)]
                : interiorCategories[rng.Next(interiorCategories.Length)];
        }
        usedCategories.Add(category);

        var photos = photosByCategory[category];
        var photo  = photos[rng.Next(photos.Length)];

        images.Add(new PropertyImage
        {
            Id           = Guid.NewGuid(),
            PropertyId   = propertyIds[i],
            ImageUrl     = photo.Url,
            Caption      = photo.Caption,
            DisplayOrder = j + 1,
            IsPrimary    = j == 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }
}

db.PropertyImages.AddRange(images);
await db.SaveChangesAsync();
Console.WriteLine($"  {images.Count} images saved.");

// ---------------------------------------------------------------------------
// Listings (500, mix of rent/sale)
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding 500 listings...");
var listingPropertyIds = propertyIds.OrderBy(_ => rng.Next()).Take(500).ToArray();
var listings = new List<Listing>();

for (int i = 0; i < 500; i++)
{
    var propId   = listingPropertyIds[i];
    var prop     = properties.First(p => p.Id == propId);
    var agentId  = agentIds[rng.Next(agentIds.Length)];
    var isRent   = i < 300; // 300 rent, 200 sale
    var listType = isRent ? ListingType.Rent : ListingType.Sale;

    decimal price;
    if (isRent)
    {
        price = prop.Bedrooms switch
        {
            0 or 1 => rng.Next(380, 650),
            2      => rng.Next(550, 900),
            3      => rng.Next(750, 1400),
            _      => rng.Next(1100, 2200),
        };
        // weekly rent — round to nearest 10
        price = Math.Round(price / 10) * 10;
    }
    else
    {
        price = prop.PropertyTypeId switch
        {
            2 => rng.Next(650, 1800) * 1000m,   // apartment
            3 => rng.Next(900, 2200) * 1000m,   // townhouse
            4 => rng.Next(850, 2000) * 1000m,   // villa
            5 => rng.Next(500, 1500) * 1000m,   // land
            _ => rng.Next(1100, 4500) * 1000m,  // house
        };
    }

    var listedAt = now.AddDays(-rng.Next(1, 180));

    listings.Add(new Listing
    {
        Id              = Guid.NewGuid(),
        PropertyId      = propId,
        AgencyId        = agencyId,
        AgentId         = agentId,
        ListingType     = listType,
        Status          = ListingStatus.Active,
        Price           = price,
        ListedAtUtc     = listedAt,
        AvailableFromUtc = isRent ? listedAt.AddDays(rng.Next(7, 30)) : null,
        IsPublished     = true,
        IsDeleted       = false,
        CreatedAtUtc    = listedAt,
        UpdatedAtUtc    = now,
    });
}

db.Listings.AddRange(listings);
await db.SaveChangesAsync();
Console.WriteLine("  Listings saved.");

// ---------------------------------------------------------------------------
// Sydney CBD (2000) — 100 dedicated properties + listings
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding 100 Sydney CBD properties...");

const int sydneyCount = 100;
var sydneyPropertyIds = Enumerable.Range(0, sydneyCount).Select(_ => Guid.NewGuid()).ToArray();
var sydneyStreets = new[]
{
    "George St", "Pitt St", "Castlereagh St", "Elizabeth St", "Kent St",
    "York St", "Clarence St", "Sussex St", "Darling Dr", "Market St",
    "King St", "Hunter St", "Bond St", "Grosvenor St", "Bridge St",
};

var sydneyProperties = new List<Property>();
for (int i = 0; i < sydneyCount; i++)
{
    // CBD is almost exclusively apartments
    var typeId    = rng.Next(10) < 8 ? 2 : 3; // 80% apartment, 20% townhouse
    var agentId   = agentIds[rng.Next(agentIds.Length)];
    var bedrooms  = rng.Next(1, 4);
    var bathrooms = Math.Max(1, bedrooms - rng.Next(0, 2));
    var carSpaces = rng.Next(0, 2);
    var typeName  = typeId == 2 ? "Apartment" : "Townhouse";

    sydneyProperties.Add(new Property
    {
        Id              = sydneyPropertyIds[i],
        PropertyTypeId  = typeId,
        AgencyId        = agencyId,
        AgentId         = agentId,
        Title           = $"{bedrooms}BR {typeName} in Sydney CBD",
        Description     = $"Premium {typeName.ToLower()} in the heart of Sydney CBD. Stunning city views, modern finishes and walking distance to everything Sydney has to offer.",
        Bedrooms        = bedrooms,
        Bathrooms       = bathrooms,
        CarSpaces       = carSpaces,
        BuildingSizeSqm = rng.Next(50, 180),
        YearBuilt       = rng.Next(1995, 2024),
        IsActive        = true,
        CreatedAtUtc    = now.AddDays(-rng.Next(1, 365)),
        UpdatedAtUtc    = now,
    });
}
db.Properties.AddRange(sydneyProperties);
await db.SaveChangesAsync();

var sydneyAddresses = sydneyPropertyIds.Select((id, i) => new PropertyAddress
{
    PropertyId   = id,
    AddressLine1 = $"{rng.Next(1, 300)} {sydneyStreets[rng.Next(sydneyStreets.Length)]}",
    Suburb       = "Sydney",
    State        = "NSW",
    Postcode     = "2000",
    Country      = "Australia",
    Latitude     = -33.8688m + (decimal)(rng.NextDouble() * 0.01 - 0.005),
    Longitude    = 151.2093m + (decimal)(rng.NextDouble() * 0.01 - 0.005),
    CreatedAtUtc = now,
    UpdatedAtUtc = now,
}).ToList();
db.PropertyAddresses.AddRange(sydneyAddresses);
await db.SaveChangesAsync();

var sydneyImages = new List<PropertyImage>();
foreach (var propId in sydneyPropertyIds)
{
    var count = rng.Next(3, 6);
    var usedCats = new HashSet<string>();
    for (int j = 0; j < count; j++)
    {
        string cat;
        if (j == 0)
        {
            cat = "exterior";
        }
        else
        {
            var available = interiorCategories.Where(c => !usedCats.Contains(c)).ToArray();
            cat = available.Length > 0 ? available[rng.Next(available.Length)] : interiorCategories[rng.Next(interiorCategories.Length)];
        }
        usedCats.Add(cat);
        var photo = photosByCategory[cat][rng.Next(photosByCategory[cat].Length)];
        sydneyImages.Add(new PropertyImage
        {
            Id           = Guid.NewGuid(),
            PropertyId   = propId,
            ImageUrl     = photo.Url,
            Caption      = photo.Caption,
            DisplayOrder = j + 1,
            IsPrimary    = j == 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }
}
db.PropertyImages.AddRange(sydneyImages);
await db.SaveChangesAsync();

var sydneyListings = new List<Listing>();
for (int i = 0; i < sydneyCount; i++)
{
    var prop     = sydneyProperties[i];
    var agentId  = agentIds[rng.Next(agentIds.Length)];
    var isRent   = i < 60; // 60 rent, 40 sale
    var listType = isRent ? ListingType.Rent : ListingType.Sale;
    var listedAt = now.AddDays(-rng.Next(1, 120));

    decimal price;
    if (isRent)
    {
        price = prop.Bedrooms switch
        {
            1 => rng.Next(600, 900),
            2 => rng.Next(850, 1400),
            _ => rng.Next(1300, 2200),
        };
        price = Math.Round(price / 10m) * 10;
    }
    else
    {
        price = prop.Bedrooms switch
        {
            1 => rng.Next(750, 1200) * 1000m,
            2 => rng.Next(1100, 2000) * 1000m,
            _ => rng.Next(1800, 3500) * 1000m,
        };
    }

    sydneyListings.Add(new Listing
    {
        Id               = Guid.NewGuid(),
        PropertyId       = sydneyPropertyIds[i],
        AgencyId         = agencyId,
        AgentId          = agentId,
        ListingType      = listType,
        Status           = ListingStatus.Active,
        Price            = price,
        ListedAtUtc      = listedAt,
        AvailableFromUtc = isRent ? listedAt.AddDays(rng.Next(7, 21)) : null,
        IsPublished      = true,
        IsDeleted        = false,
        CreatedAtUtc     = listedAt,
        UpdatedAtUtc     = now,
    });
}
db.Listings.AddRange(sydneyListings);
listings.AddRange(sydneyListings);
await db.SaveChangesAsync();
Console.WriteLine($"  Sydney CBD: {sydneyCount} properties, {sydneyListings.Count} listings (rent: {sydneyListings.Count(l => l.ListingType == ListingType.Rent)}, sale: {sydneyListings.Count(l => l.ListingType == ListingType.Sale)}) saved.");

// ---------------------------------------------------------------------------
// Suburb-specific batches — 100 properties + listings each
// ---------------------------------------------------------------------------
var suburbBatches = new[]
{
    ("Parramatta",   "NSW", "2150", -33.8148m, 151.0017m),
    ("North Sydney", "NSW", "2060", -33.8393m, 151.2077m),
    ("Chatswood",    "NSW", "2067", -33.7970m, 151.1816m),
    ("Castle Hill",  "NSW", "2154", -33.7313m, 150.9997m),
};

var allSuburbBatchListings = new List<Listing>();

foreach (var (batchSuburb, batchState, batchPostcode, batchLat, batchLng) in suburbBatches)
{
    Console.WriteLine($"Seeding 100 properties for {batchSuburb}...");
    const int batchCount = 100;
    var batchPropertyIds = Enumerable.Range(0, batchCount).Select(_ => Guid.NewGuid()).ToArray();

    // Property type mix varies by suburb character
    bool isUrban = batchSuburb is "North Sydney" or "Chatswood";
    bool isCbd   = batchSuburb is "Parramatta";

    var batchProperties = new List<Property>();
    for (int i = 0; i < batchCount; i++)
    {
        int typeId;
        if (isUrban || isCbd)
            typeId = rng.Next(10) < 7 ? 2 : 3;        // 70% apartment, 30% townhouse
        else
            typeId = rng.Next(10) < 5 ? 1              // Castle Hill: 50% house
                   : rng.Next(10) < 7 ? 3 : 2;         //              30% townhouse, 20% apt

        var agentId   = agentIds[rng.Next(agentIds.Length)];
        var typeName  = typeId switch { 1 => "House", 3 => "Townhouse", _ => "Apartment" };
        var bedrooms  = typeId == 2 ? rng.Next(1, 4) : rng.Next(2, 5);
        var bathrooms = Math.Max(1, bedrooms - rng.Next(0, 2));
        var carSpaces = rng.Next(typeId == 2 ? 0 : 1, 3);

        batchProperties.Add(new Property
        {
            Id              = batchPropertyIds[i],
            PropertyTypeId  = typeId,
            AgencyId        = agencyId,
            AgentId         = agentId,
            Title           = $"{bedrooms}BR {typeName} in {batchSuburb}",
            Description     = $"Beautifully presented {typeName.ToLower()} in sought-after {batchSuburb}. Close to transport, shops and schools — an outstanding opportunity in a tightly held area.",
            Bedrooms        = bedrooms,
            Bathrooms       = bathrooms,
            CarSpaces       = carSpaces,
            LandSizeSqm     = typeId == 1 ? rng.Next(300, 700) : null,
            BuildingSizeSqm = typeId != 5 ? rng.Next(70, 300) : null,
            YearBuilt       = rng.Next(1980, 2024),
            IsActive        = true,
            CreatedAtUtc    = now.AddDays(-rng.Next(1, 365)),
            UpdatedAtUtc    = now,
        });
    }
    db.Properties.AddRange(batchProperties);
    await db.SaveChangesAsync();

    var batchAddresses = batchPropertyIds.Select((id, i) => new PropertyAddress
    {
        PropertyId   = id,
        AddressLine1 = $"{rng.Next(1, 250)} {sydneyStreets[rng.Next(sydneyStreets.Length)]}",
        Suburb       = batchSuburb,
        State        = batchState,
        Postcode     = batchPostcode,
        Country      = "Australia",
        Latitude     = batchLat + (decimal)(rng.NextDouble() * 0.01 - 0.005),
        Longitude    = batchLng + (decimal)(rng.NextDouble() * 0.01 - 0.005),
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    }).ToList();
    db.PropertyAddresses.AddRange(batchAddresses);
    await db.SaveChangesAsync();

    var batchImages = new List<PropertyImage>();
    foreach (var propId in batchPropertyIds)
    {
        var count = rng.Next(3, 6);
        var usedCats = new HashSet<string>();
        for (int j = 0; j < count; j++)
        {
            string cat;
            if (j == 0)
            {
                cat = "exterior";
            }
            else
            {
                var available = interiorCategories.Where(c => !usedCats.Contains(c)).ToArray();
                cat = available.Length > 0 ? available[rng.Next(available.Length)] : interiorCategories[rng.Next(interiorCategories.Length)];
            }
            usedCats.Add(cat);
            var photo = photosByCategory[cat][rng.Next(photosByCategory[cat].Length)];
            batchImages.Add(new PropertyImage
            {
                Id           = Guid.NewGuid(),
                PropertyId   = propId,
                ImageUrl     = photo.Url,
                Caption      = photo.Caption,
                DisplayOrder = j + 1,
                IsPrimary    = j == 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
    }
    db.PropertyImages.AddRange(batchImages);
    await db.SaveChangesAsync();

    var batchListings = new List<Listing>();
    for (int i = 0; i < batchCount; i++)
    {
        var prop     = batchProperties[i];
        var agentId  = agentIds[rng.Next(agentIds.Length)];
        var isRent   = i < 60;
        var listType = isRent ? ListingType.Rent : ListingType.Sale;
        var listedAt = now.AddDays(-rng.Next(1, 180));

        decimal price;
        if (isRent)
        {
            price = prop.Bedrooms switch
            {
                1 => rng.Next(450, 750),
                2 => rng.Next(650, 1100),
                3 => rng.Next(900, 1500),
                _ => rng.Next(1200, 2000),
            };
            price = Math.Round(price / 10m) * 10;
        }
        else
        {
            price = prop.PropertyTypeId switch
            {
                1 => rng.Next(1200, 3500) * 1000m,  // house
                3 => rng.Next(900, 1800) * 1000m,   // townhouse
                _ => rng.Next(600, 1500) * 1000m,   // apartment
            };
        }

        batchListings.Add(new Listing
        {
            Id               = Guid.NewGuid(),
            PropertyId       = batchPropertyIds[i],
            AgencyId         = agencyId,
            AgentId          = agentId,
            ListingType      = listType,
            Status           = ListingStatus.Active,
            Price            = price,
            ListedAtUtc      = listedAt,
            AvailableFromUtc = isRent ? listedAt.AddDays(rng.Next(7, 21)) : null,
            IsPublished      = true,
            IsDeleted        = false,
            CreatedAtUtc     = listedAt,
            UpdatedAtUtc     = now,
        });
    }
    db.Listings.AddRange(batchListings);
    listings.AddRange(batchListings);
    allSuburbBatchListings.AddRange(batchListings);
    await db.SaveChangesAsync();
    Console.WriteLine($"  {batchSuburb}: {batchCount} properties, {batchListings.Count} listings (rent: {batchListings.Count(l => l.ListingType == ListingType.Rent)}, sale: {batchListings.Count(l => l.ListingType == ListingType.Sale)}) saved.");
}

// ---------------------------------------------------------------------------
// Users (system + tenant prospects for enquiries)
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding users...");

var systemUserId = Guid.NewGuid();
db.Users.Add(new User
{
    Id           = systemUserId,
    Email        = "system@nexus.com.au",
    FirstName    = "System",
    LastName     = "Seed",
    IsActive     = false,
    CreatedAtUtc = now,
    UpdatedAtUtc = now,
});

var tenantData = new[]
{
    ("oliver.brown@gmail.com",    "Oliver",   "Brown"),
    ("charlotte.smith@gmail.com", "Charlotte","Smith"),
    ("william.jones@outlook.com", "William",  "Jones"),
    ("amelia.taylor@gmail.com",   "Amelia",   "Taylor"),
    ("jack.wilson@outlook.com",   "Jack",     "Wilson"),
    ("isla.moore@gmail.com",      "Isla",     "Moore"),
    ("henry.davis@gmail.com",     "Henry",    "Davis"),
    ("grace.martin@outlook.com",  "Grace",    "Martin"),
    ("thomas.white@gmail.com",    "Thomas",   "White"),
    ("sophie.harris@gmail.com",   "Sophie",   "Harris"),
};

var tenantIds = tenantData.Select(_ => Guid.NewGuid()).ToArray();
for (int i = 0; i < tenantData.Length; i++)
{
    var (email, first, last) = tenantData[i];
    db.Users.Add(new User
    {
        Id           = tenantIds[i],
        Email        = email,
        FirstName    = first,
        LastName     = last,
        IsActive     = true,
        CreatedAtUtc = now.AddDays(-rng.Next(30, 365)),
        UpdatedAtUtc = now,
    });
}
await db.SaveChangesAsync();

// ---------------------------------------------------------------------------
// Inspection Slots (for rent listings only, 2–3 slots each)
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding inspection slots...");

var rentListings = listings.Where(l => l.ListingType == ListingType.Rent).ToList();
var slots = new List<InspectionSlot>();
var slotDays = new[] { 6, 7 }; // Saturday, Sunday
var slotTimes = new[] { 9, 10, 11, 12 }; // hours

foreach (var listing in rentListings)
{
    var slotCount = rng.Next(2, 4);
    for (int s = 0; s < slotCount; s++)
    {
        var daysAhead = rng.Next(3, 30);
        var baseDate  = now.Date.AddDays(daysAhead);
        var hour      = slotTimes[rng.Next(slotTimes.Length)];
        var startAt   = new DateTimeOffset(baseDate.Year, baseDate.Month, baseDate.Day, hour, 0, 0, TimeSpan.Zero);

        slots.Add(new InspectionSlot
        {
            Id          = Guid.NewGuid(),
            ListingId   = listing.Id,
            PropertyId  = listing.PropertyId,
            AgentId     = listing.AgentId ?? agentIds[0],
            UserId      = systemUserId,
            StartAtUtc  = startAt,
            EndAtUtc    = startAt.AddMinutes(30),
            Capacity    = rng.Next(5, 15),
            Status      = InspectionSlotStatus.Open,
            IsDeleted   = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }
}

db.InspectionSlots.AddRange(slots);
await db.SaveChangesAsync();
Console.WriteLine($"  {slots.Count} inspection slots saved.");

// ---------------------------------------------------------------------------
// Enquiries (50 records)
// ---------------------------------------------------------------------------
Console.WriteLine("Seeding enquiries...");

var enquiryMessages = new[]
{
    "Hi, I'm very interested in this property. Could you please let me know if it's still available and arrange an inspection?",
    "I would like to find out more about this property. What are the strata fees, and is parking included?",
    "Could you please provide more details about the lease terms? I'm looking to move in within the next 2–3 weeks.",
    "I drove past this property and it looks great. Can you tell me a bit more about the neighbourhood and nearby schools?",
    "Is this property pet-friendly? I have a small dog and would love to arrange a viewing if possible.",
    "Hi, I noticed this listing just came up. I'm very keen — is there an inspection scheduled this weekend?",
    "Could you send me more photos of the kitchen and bathrooms? We're very interested and would love to arrange a time to inspect.",
    "We're a young family relocating from Melbourne. Is this property still available? What's the earliest move-in date?",
    "I'm a first home buyer and very interested in this property. Is there any flexibility on the asking price?",
    "Hi, I'd love to arrange a private inspection at your earliest convenience. This property ticks all our boxes.",
};

var enquiryStatuses = new[] { EnquiryStatus.New, EnquiryStatus.Read, EnquiryStatus.Responded };
var enquiries = new List<Enquiry>();
var enquiryListings = listings.OrderBy(_ => rng.Next()).Take(50).ToList();

for (int i = 0; i < 50; i++)
{
    var listing    = enquiryListings[i];
    var tenantId   = tenantIds[rng.Next(tenantIds.Length)];
    var createdAt  = now.AddDays(-rng.Next(1, 60));
    var status     = enquiryStatuses[rng.Next(enquiryStatuses.Length)];

    enquiries.Add(new Enquiry
    {
        Id              = Guid.NewGuid(),
        UserId          = tenantId,
        PropertyId      = listing.PropertyId,
        ListingId       = listing.Id,
        AgentId         = listing.AgentId,
        Message         = enquiryMessages[rng.Next(enquiryMessages.Length)],
        Status          = status,
        CreatedAtUtc    = createdAt,
        UpdatedAtUtc    = createdAt,
        RespondedAtUtc  = status == EnquiryStatus.Responded ? createdAt.AddHours(rng.Next(1, 48)) : null,
    });
}

db.Enquiries.AddRange(enquiries);
await db.SaveChangesAsync();
Console.WriteLine($"  {enquiries.Count} enquiries saved.");

Console.WriteLine();
Console.WriteLine("Seed complete.");
Console.WriteLine($"  Property types   : {propertyTypes.Count}");
Console.WriteLine($"  Agencies         : 1");
Console.WriteLine($"  Agents           : {agentIds.Length}");
Console.WriteLine($"  Users            : {tenantIds.Length} tenants + 1 system");
Console.WriteLine($"  Properties       : {totalProperties + sydneyCount} ({totalProperties} metro + {sydneyCount} Sydney CBD)");
Console.WriteLine($"  Addresses        : {addresses.Count + sydneyAddresses.Count}");
Console.WriteLine($"  Images           : {images.Count + sydneyImages.Count}");
Console.WriteLine($"  Listings         : {listings.Count}  (rent: {listings.Count(l => l.ListingType == ListingType.Rent)}, sale: {listings.Count(l => l.ListingType == ListingType.Sale)})");
Console.WriteLine($"  Inspection slots : {slots.Count}");
Console.WriteLine($"  Enquiries        : {enquiries.Count}");
Console.WriteLine($"  Inspection bookings: 0 (intentionally empty)");
