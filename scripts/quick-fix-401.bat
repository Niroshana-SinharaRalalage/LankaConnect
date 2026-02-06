@echo off
echo.
echo ===================================================
echo    FIX FOR 401 AUTHENTICATION ERRORS
echo ===================================================
echo.
echo This will open the browser storage cleaner to fix your 401 errors.
echo.
echo INSTRUCTIONS:
echo 1. Click "Clear All Auth Data" in the browser window
echo 2. Login again at http://localhost:3000/login
echo 3. Navigate to any event page - 401 errors will be GONE!
echo.
pause
start "" "http://localhost:3000/clear-auth.html"
echo.
echo Browser cleaner opened! Follow the instructions above.
echo.
pause