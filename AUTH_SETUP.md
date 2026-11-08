# Հրավերներ, դերեր և մուտքի հաստատում

## Տվյալների բազա

Ստեղծեք դատարկ PostgreSQL բազա։ DataGrip-ում բացեք այդ բազան և հերթով գործարկեք՝

1. `StudentMonitoring.Server/Database/000_initial_schema.sql`
2. `StudentMonitoring.Server/Database/001_auth.sql`
3. `StudentMonitoring.Server/Database/002_invitations_and_login_codes.sql`

Երկրորդ և երրորդ ֆայլերն ավելացնում են email հաստատման, մեկանգամյա հրավերների, բաժնի կապերի և admin/SuperAdmin մուտքի կոդերի աղյուսակները։ `003`, `005` և `006` ֆայլերը միայն demo/test տվյալներ են ավելացնում կամ փոփոխում։

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=studentmonitoringproject;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
```

## SMTP

SMTP գաղտնաբառը պահեք .NET User Secrets-ում, ոչ թե կոդում կամ `appsettings` ֆայլում։ Օրինակ՝ Gmail-ի համար փոխարինեք `YOUR_GOOGLE_APP_PASSWORD`-ը ձեր Google App Password-ով.

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "Email:SmtpPort" "587" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "Email:Username" "your-address@gmail.com" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "Email:Password" "YOUR_GOOGLE_APP_PASSWORD" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "Email:FromAddress" "your-address@gmail.com" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "Email:FromName" "Ուսանողական առաջադիմության համակարգ" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "Email:EnableSsl" "true" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
```

Տեղական հրավերների հղումներն ուղղվում են `http://localhost:5173` հասցեին։ Մեկ այլ հոսթի դեպքում փոխեք `Email:InvitationBaseUrl`-ը։

## Սկզբնական SuperAdmin

Եթե բազայում դեռ SuperAdmin չկա, API-ի User Secrets-ում լրացրեք սկզբնական հաշվի էլ. հասցեն և առնվազն 8 նիշանոց գաղտնաբառը։ Առաջին գործարկման ժամանակ հաշիվը կստեղծվի միայն այն դեպքում, երբ մեկ այլ SuperAdmin արդեն չկա։

```powershell
dotnet user-secrets set "BootstrapSuperAdmin:Email" "your-admin@example.com" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
dotnet user-secrets set "BootstrapSuperAdmin:Password" "YOUR_SECURE_PASSWORD" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
```

## Հաշիվների ստեղծման հոսք

- Բաց գրանցումն անջատված է․ նոր հաշիվները ստեղծվում են հրավերով։
- SuperAdmin-ը ստեղծում է բաժիններ և ադմին հրավիրում՝ ընտրելով նրա բաժինները։ Հրավերի հղումը մեկանգամյա է և գործում է 7 օր։
- Ադմինը կարող է հրավիրել միայն իրեն նշանակված բաժինների դասախոսներին և ուսանողներին։ Նույն բաժինները կապվում են նոր հաշվի հետ։ Ադմինը կարող է տեսնել իր անձնակազմը և ակտիվացնել կամ անջատել նրանց հաշիվները։
- Հրավիրված անձը հղումով լրացնում է անունը, ազգանունը և գաղտնաբառը։ Դերը վերցվում է հրավերից, օգտատերը չի ընտրում իր դերը։
- Admin և SuperAdmin մուտք գործելուց հետո ստանում են 10 րոպե վավեր 6-նիշ կոդ։ Կոդը կարելի է փորձել առավելագույնը 5 անգամ, նոր կոդը ուղարկվում է 1 րոպեից ոչ շուտ։
- Անջատված հաշիվը API հաջորդ հարցման ժամանակ դուրս է գրվում համակարգից։

