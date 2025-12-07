create proc pCountry;2
@id int
as 
select 
	id,
	name,
	capital
from country
where id = @id