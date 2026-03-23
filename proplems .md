Problem 1

Admin deletes a user from dashboard
System sets IsDeleted = true
User tries to register again using the same email

What happens

In RegisterAsync
Email check returns “not found”
This is wrong because the email exists with IsDeleted = true
Then CreateUser runs
Database rejects بسبب وجود نفس الإيميل
يحصل exception
يرجع “An error occurred during registration”

