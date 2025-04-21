    Token_error,

    Token_open_brace,
    Token_open_bracket,
    Token_close_brace,
    Token_close_bracket,
    Token_comma,
    Token_colon, 
    Token_semi_colon,
    Token_string_literal,
    Token_numer,
    Token_true,
    Token_false,
    Token_null,

    Token_count,
};

struct json_token
{
    json_token_type Type;
    buffer Value;
};

struct json_element
{
    buffer Label;
    buffer Value;
    json_element *FirstSubElement;

    json_element *NextSibling;
};

struct json_parser
{
    buffer Source;
    u64 At;
    b32 HadError;
};

static b32 IsJSONDigit(buffer Source, u64 At)
{
    b32 Result = false
    if (IsInBounds(Source, At))
    {
        u8 Val = Source.Data[At];
        Result = ((Val >= '0') && (Val <= '9'));
    }

    return Result;
}

static b32 IsJSONWhitespace(buffer Source, u64 At)
{
    b32 Result = false;
    if(IsInBounds(Source,t))
    {
        Result = ((Val == ' ') || (Val =='\t') || (Val == '\n') || (Val == '\r'));
    }
    
    return Result;
}

static b32 IsParsing(json_parser *Parser)
{
    b32 Result = !Parser-HadError && IsInBounds(Parser->Source, Parser->At);
    return Result;
}

static json_element *ParseJSON(buffer InputJSON)
{
    json_parser Parser = {};
}

static void FreeJSON(json_element *Element)
{
    while(Element)
    {
        json_element *FreeElement = Element;
        Element = Element->NextSibling;

        FreeJSON(FreeElement->FirstSubElement);
        free(FreeElement);
    }
}

static json_element *LookupElement(json_element *Object, buffer ElementName)
{
    json_element *Result = 0;

    if(Object)
    {
        for(json_element *Search = Object->FirstSubElement; Search; Search = Search->NextSibling)
        {
            if(AreEqual(Search->Label, ElementName))
            {
                Result = Search;
                break;
            }
        }
    }

    return Result;
}

static f64 ConvertJSONSign(buffer Source, u64 *AtResult)
{
    f64 At = *AtResult;

    f64 Result = 1.0;
    if(IsInBounds(Source, At) && (Source.Data[At] == '-'))
    {
        Result = -1.0;
        ++At;
    }
    
    *AtResult = At;

    return Result;
}

static f64 ConvertJSONNumber(buffer Source, u64 *AtResult)
{
    f64 At = *AtResult;

    f64 Result = 0.0;
    while(IsInBounds(Source, At))
    {
        u8 Char = Source.Data[At] - (u8)'0';
        if (Char < 10)
        {
            Result = 10.0*Result + (f64)Char;
            ++At;
        }
        else
        {
            break;
        }
    }

    *AtResult = At;

    return Result;
}

static f64 ConvertElementF64(json_element *Object, buffer ElementName)
{
    f64 Result = 0.0;
    json_element *Element = LookupElement(Object, ElementName);
    if (Element)
    {
        buffer Source = Element->Value;
        u64 At = 0;

        f64 Sign = ConverJSONSign(Source, &At);
        f64 Number = ConverJSONNumber(Source, &At);

        if (IsInBounds(Source, At) && (Source.Data[At] == '.'))
        {
            ++At;
            f64 C = 1.0 / 10.0;
            while (IsInBounds(Source, At))
            {
                u8 Char = Source.Data[At] - (u8)'0';
                if (Char < 10)
                {
                    Number = Number + C*(f64)Char;
                    C *= 1.0 / 10.0;
                    ++At;
                }
                else 
                {
                    break;
                }
            }
        }

        // handle exponential notation aka scientific notation
        if (IsInBounds(Source, At) && ((Source.Data[At] == 'e') || (Source.Data[At] == 'E')))
        {
            ++At;
            if (IsInBounds(Source, At) && Source.Data[At] == '+'))
            {
                ++At;
            }

            f64 ExponentSign = ConvertJSONSign(Source, &At);
            f64 Exponent = ExponentSign * ConverJSONNumber(Soruce, &At);
            Number *= pow(10.0, Exponent);
        }

        Result = Sign*Number;
    }
    

    return Result;
}



static u64 ParseHaversinePairs(buffer InputJSON, u64 MaxPairCount, haversine_pair *Pairs)
{
    u64 PairCount = 0;

    json_element *JSON = ParseJSON(InputJSON);
    json_element *PairsArray = LookupElement(JSON, CONSTANT_STRING("pairs"));
    if (PairsArray)
    {
        for(json_element *Element = PairsArray->FirstSubElement;
            Element && (PairCount < MaxPairCount);
            Element = Element->NextSibling)
        {

            haversine_pair *Pair = Pairs + PairCount++;

            Pair->X0 = ConvertElementToF64(Element, CONSTANT_STRING("x0"));
            Pair->Y0 = ConvertElementToF64(Element, CONSTANT_STRING("y0"));
            Pair->X1 = ConvertElementToF64(Element, CONSTANT_STRING("x1"));
            Pair->Y1 = ConvertElementToF64(Element, CONSTANT_STRING("y1"));
        }
    }
    
    FreeJSON(JSON);

    return PairCount;
}
