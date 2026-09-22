for /L %%p in (3000,1,3006) do for /f "tokens=5" %%a in ('netstat -ano ^| findstr :%%p') do taskkill /PID %%a /F
