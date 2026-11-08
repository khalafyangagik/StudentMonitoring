# Ուսանողական առաջադիմության մոնիթորինգի և վերլուծության համակարգ

Կրթական համակարգի կառավարման վեբ հավելված՝ ուսանողների առաջադիմությունը, հաճախելիությունը, դասաժամերն ու դերային աշխատանքային հոսքերը կառավարելու և վերլուծելու համար։

## Տեխնոլոգիաներ

- **Front-end:** React, Vite, JavaScript և CSS
- **Back-end:** ASP.NET Core Web API (.NET 8), C# և ADO.NET
- **Տվյալների բազա:** PostgreSQL՝ Npgsql կապով
- **Անվտանգություն:** cookie-ով նույնականացում, դերերի ստուգում, գաղտնաբառերի hash և հրավեր/մուտքի հաստատման հոսքեր

## Դերեր

- **SuperAdmin:** ստեղծում է բաժիններ և հրավիրում է ադմինիստրատորների՝ բաժինների հասանելիությամբ։
- **Ադմինիստրատոր:** կառավարում է իրեն վերագրված բաժինների ուսանողներին, դասախոսներին և ուսումնական տվյալները։
- **Դասախոս:** տեսնում է իր դասընթացներն ու խմբերը, գրանցում է գնահատականներ և հաճախելիություն։
- **Ուսանող:** տեսնում է իր գնահատականները, հաճախելիությունն ու ուսումնական առաջընթացը։

## Գործարկում տեղական միջավայրում

Պետք են .NET 8 SDK, Node.js-ի Vite-ի հետ համատեղելի տարբերակ և աշխատող PostgreSQL։

1. Կլոնավորեք repository-ն և բացեք `StudentMonitoring.sln`-ը Visual Studio-ում։
2. Ստեղծեք տվյալների բազան և լրացրեք դրա կապը User Secrets-ում։

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=studentmonitoringproject;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project "StudentMonitoring.Server/StudentMonitoring.Server.csproj"
   ```

3. Գործարկեք տվյալների բազայի հիմնական schema-ն, ապա անհրաժեշտ authentication script-երը `StudentMonitoring.Server/Database` պանակից։ `003`, `005` և `006` ֆայլերը demo/test տվյալների համար են․ դրանք production բազայում մի գործարկեք։ Հերթականության մանրամասները նշված են [AUTH_SETUP.md](AUTH_SETUP.md)-ում։
4. Տեղադրեք client-ի կախվածությունները և գործարկեք հավելվածը.

   ```powershell
   cd studentmonitoring.client
   npm install
   npm run dev
   ```

5. ASP.NET Core API-ն գործարկեք Visual Studio-ից՝ `http` profile-ով, կամ առանձին տերմինալում.

   ```powershell
   dotnet run --project StudentMonitoring.Server/StudentMonitoring.Server.csproj --launch-profile http
   ```

Client-ը հասանելի է `http://localhost:5173`, API-ն՝ `http://localhost:5080` հասցեով։

## Գաղտնի կարգավորումներ

Տվյալների բազայի գաղտնաբառը, SMTP տվյալները և սկզբնական SuperAdmin-ի գաղտնաբառը պահեք .NET User Secrets-ում կամ deployment environment variables-ում։ Մի ավելացրեք իրական գաղտնաբառեր, token-ներ կամ production տվյալներ source control-ում։ Gmail SMTP-ի և հրավերների կազմաձևումը ներկայացված է [AUTH_SETUP.md](AUTH_SETUP.md)-ում։

## Տվյալների բազայի ֆայլեր

`StudentMonitoring.Server/Database`-ում պահվում են authentication-ի և հրավերների SQL փոփոխությունները, ինչպես նաև միայն demo/test միջավայրի seed/update script-երը։ Դրանք մի գործարկեք ձեր իրական տվյալների բազայում առանց նախապես բազան պահուստավորելու և script-երի ազդեցությունը ստուգելու։

