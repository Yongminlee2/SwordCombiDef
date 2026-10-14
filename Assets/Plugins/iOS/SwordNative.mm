// Values only iOS knows, handed to C#. The C# side is Runtime/IosNative.cs.
#import <Foundation/Foundation.h>

extern "C" {

// Value of the launch argument "-<key> <value>". iOS puts such arguments in
// NSUserDefaults and does not pass them to Environment.GetCommandLineArgs().
// IL2CPP frees the returned string. NULL when absent.
char* SwordLaunchArgument(const char* key)
{
    NSString* value = [NSUserDefaults.standardUserDefaults stringForKey:@(key)];
    return value != nil ? strdup(value.UTF8String) : NULL;
}

}
