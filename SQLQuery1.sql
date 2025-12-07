create table country
(
	id int primary key identity,
	name nvarchar(200) not null,
	capital nvarchar(200)
)

insert into country (name, capital)
values (N'Казахстан',N'Астана'),
(N'Россия', N'Москва'),(N'Китай', N'Пекин'),(N'Германия', N'Берлин')

select country 

create proc pCountry
as
	select id,
	name,
	capital
from country
order by name
