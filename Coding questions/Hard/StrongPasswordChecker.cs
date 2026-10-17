namespace SotiCodingQuestions.Hard;

public static class StrongPasswordChecker
{
   
    public static int MinimumEdits(string str)
    {
              //  string str= "111a11";
         int missingType=3;
        int changesNeeded=0;
        int output=0;
      if(str.Any(x => x >= 'a' && x<= 'z'))
      {
          missingType--;
          
      }
     if(str.Any(x => (x >= 'A' && x<= 'Z')))
      {
          missingType--;
          
      }
      if(str.Any(x => x >= '0' && x <= '9'))
      {
          missingType--;
          
      }
        Console.WriteLine("missingType "+missingType);
        
        var dict=new Dictionary<string,int>();
        for(int i=0;i < str.Length;i++)
        {
            if(dict.TryGetValue(str[i].ToString(),out int a) && str[i-1] == str[i])
            {
              dict[str[i].ToString()]+=1;  
            }
            else if(dict.TryGetValue(str[i].ToString(),out int b))
            {
                
                dict.Add(str[i].ToString()+i,1);
            }
            else
            {
                dict.Add(str[i].ToString(),1);
            }
        }

         Dictionary<string,int> res = dict.Where(x=>x.Value > 2)
             .ToDictionary(y => y.Key, y=>y.Value);
             //.ToDictionary(y => y.Key, y => y.Value);
        int replacements = res.Sum(x=>x.Value/3);
        int deletions=(str.Length-20);
        int deletions1=(str.Length-20);
        
        if(str.Length < 6)
        {
            return(Math.Max(missingType,(6-str.Length)));
        }
        else if(str.Length > 20)
        {
            int finalreplacement=0;
            foreach(var key in res.Keys.ToArray())
            {
             if(res[key] % 3==0 && deletions>0)
             {
                  res[key]--;
                 replacements--;
                 deletions--;
             }
                
            }

             foreach(var key in res.Keys.ToArray())
            {
                 Console.WriteLine("before"+res[key]);
             if(res[key] % 3==1 && deletions>=2)
             {
                  res[key]=res[key]-2;
                 replacements--;
                 deletions-=2;
             }

                Console.WriteLine("After"+res[key]);
            }
            replacements -= deletions / 3;
        Console.WriteLine("replacements "+replacements);
            
      Console.WriteLine("deletions "+deletions1);
            
        return deletions1 + Math.Max(missingType, replacements);
        }
        else 
        {
        return(Math.Max(missingType,replacements));
        }
    }

     public static int MinimumEdits(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        int missingTypes = 3;
        if (password.Any(character => character is >= 'a' and <= 'z'))
        {
            missingTypes--;
        }

        if (password.Any(character => character is >= 'A' and <= 'Z'))
        {
            missingTypes--;
        }

        if (password.Any(character => character is >= '0' and <= '9'))
        {
            missingTypes--;
        }

        var repeatedRunLengths = new List<int>();
        for (int index = 0; index < password.Length;)
        {
            int runEnd = index + 1;
            while (runEnd < password.Length && password[runEnd] == password[index])
            {
                runEnd++;
            }

            int runLength = runEnd - index;
            if (runLength >= 3)
            {
                repeatedRunLengths.Add(runLength);
            }

            index = runEnd;
        }

        int replacements = repeatedRunLengths.Sum(runLength => runLength / 3);

        if (password.Length < 6)
        {
            return Math.Max(missingTypes, 6 - password.Length);
        }

        if (password.Length <= 20)
        {
            return Math.Max(missingTypes, replacements);
        }

        int deletions = password.Length - 20;
        int remainingDeletions = deletions;

        for (int index = 0; index < repeatedRunLengths.Count && remainingDeletions > 0; index++)
        {
            if (repeatedRunLengths[index] % 3 == 0)
            {
                repeatedRunLengths[index]--;
                remainingDeletions--;
                replacements--;
            }
        }

        for (int index = 0; index < repeatedRunLengths.Count && remainingDeletions >= 2; index++)
        {
            if (repeatedRunLengths[index] % 3 == 1)
            {
                repeatedRunLengths[index] -= 2;
                remainingDeletions -= 2;
                replacements--;
            }
        }

        replacements -= remainingDeletions / 3;
        return deletions + Math.Max(missingTypes, replacements);
    }
}
