# The new C#-Python connection system

Because I have not decide how to call it, I call it 'new system' in the rest here.

1. New system is extremely fast.

2. New system is extremely fast.

3. New system is synchronous. You need to run multiple .NET process for asynchronous setup

4. Usage: `dotnet run -c release mmf [Optional ID]`

   You need the ID you used to run .NET process to use it in Python process.

5. Python usage:

   ```python
   import sabber_protocol.server
   
   ID = "This is what I used to call the target C# process"
   server = sabber_protocol.server.SabberStoneServer(ID)
   ```

6. You can find out examples in the bunch of rubbish codes in the root directory of Python part.

